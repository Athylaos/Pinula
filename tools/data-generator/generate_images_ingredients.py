import io
import json
import os
import re
import time
from typing import List, Set
import httpx
from dotenv import load_dotenv
from PIL import Image
import torch
from diffusers import StableDiffusionXLPipeline, UNet2DConditionModel, EulerDiscreteScheduler
from huggingface_hub import hf_hub_download
from safetensors.torch import load_file

load_dotenv()

PINULA_API_URL = os.getenv("PINULA_API_URL", "https://localhost:7123")
ADMIN_EMAIL = os.getenv("ADMIN_EMAIL")
ADMIN_PASSWORD = os.getenv("ADMIN_PASSWORD")

CHECKPOINT_FILE = "processed_image_ids.txt"


def extract_ingredient_name(item: dict) -> str:
    names = item.get("names") or item.get("Names")

    if isinstance(names, dict):
        if names.get("en"):
            return str(names["en"])
        if names.get("cs"):
            return str(names["cs"])
        for val in names.values():
            if val:
                return str(val)

    for key in ["name", "Name", "englishName", "EnglishName", "nameEn", "NameEn"]:
        if key in item and item[key]:
            return str(item[key])

    return "food ingredient"


def clean_ingredient_name(name: str) -> str:
    name = re.sub(r"\(.*?\)", "", name)
    name = re.sub(r"\d+%\s*(fidm)?", "", name, flags=re.IGNORECASE)

    name = re.sub(r"\blyophilized\b", "freeze-dried", name, flags=re.IGNORECASE)

    parts = [p.strip() for p in name.split(",") if p.strip()]
    parts.reverse()
    clean_name = " ".join(parts)

    return re.sub(r"\s+", " ", clean_name).strip()


def load_json_file(file_path: str):
    if not os.path.exists(file_path):
        raise FileNotFoundError(f"File not found: {file_path}")
    with open(file_path, "r", encoding="utf-8") as f:
        return json.load(f)


def load_processed_ids() -> Set[str]:
    if not os.path.exists(CHECKPOINT_FILE):
        return set()
    with open(CHECKPOINT_FILE, "r", encoding="utf-8") as f:
        return set(line.strip() for line in f if line.strip())


def save_processed_id(item_id: str):
    with open(CHECKPOINT_FILE, "a", encoding="utf-8") as f:
        f.write(f"{item_id}\n")


def get_authenticated_client() -> httpx.Client:
    client = httpx.Client(base_url=PINULA_API_URL, verify=False, timeout=60.0)

    print("Authenticating admin user...")
    login_res = client.post("/users/login", json={
        "Email": ADMIN_EMAIL,
        "Password": ADMIN_PASSWORD
    })

    if login_res.status_code != 200:
        raise Exception(f"Admin authentication failed ({login_res.status_code}): {login_res.text}")

    token = login_res.json().get("token")
    if not token:
        raise Exception("Authentication response did not contain a valid JWT token.")

    client.headers.update({"Authorization": f"Bearer {token}"})
    print("Authentication successful.")
    return client


def initialize_sd_pipeline():
    print("Loading local SDXL-Lightning model onto GPU...")
    if not torch.cuda.is_available():
        raise RuntimeError("CUDA GPU acceleration is not available. Ensure PyTorch with CUDA is installed.")

    device_name = torch.cuda.get_device_name(0)
    print(f"Using GPU device: {device_name}")

    torch.cuda.empty_cache()

    base_model = "stabilityai/stable-diffusion-xl-base-1.0"
    repo = "ByteDance/SDXL-Lightning"
    ckpt = "sdxl_lightning_4step_unet.safetensors"

    ckpt_path = hf_hub_download(repo, ckpt)

    unet = UNet2DConditionModel.from_config(
        base_model,
        subfolder="unet",
        torch_dtype=torch.float16
    )
    unet.load_state_dict(load_file(ckpt_path, device="cpu"))
    unet = unet.to(dtype=torch.float16)

    pipe = StableDiffusionXLPipeline.from_pretrained(
        base_model,
        unet=unet,
        torch_dtype=torch.float16,
        variant="fp16"
    )
    pipe.scheduler = EulerDiscreteScheduler.from_config(pipe.scheduler.config, timestep_spacing="trailing")

    pipe.enable_model_cpu_offload()

    print("Model SDXL-Lightning loaded successfully.")
    return pipe


def generate_image(pipe, raw_ingredient_name: str) -> bytes:
    cleaned_name = clean_ingredient_name(raw_ingredient_name)

    prompt = (
        f"a professional studio food product photo of fresh {cleaned_name}, "
        "isolated on a seamless pure white studio background, centered composition, "
        "softbox studio lighting, unbranded plain container without any text or labels, "
        "commercial clean product shot"
    )

    negative_prompt = (
        "text, label, font, letters, writing, watermark, logo, brand, "
        "split screen, collage, grid, multiple items, pattern background, "
        "packaging text, brand name, sticker, blurry, cropped, shadows on wall"
    )

    torch.cuda.empty_cache()

    image = pipe(
        prompt=prompt,
        negative_prompt=negative_prompt,
        num_inference_steps=4,
        guidance_scale=2.0,
        width=1024,
        height=1024
    ).images[0]

    buffer = io.BytesIO()
    image.save(buffer, format="WEBP", quality=80)
    return buffer.getvalue()


def upload_image(client: httpx.Client, ingredient_id: str, image_bytes: bytes) -> bool:
    files = {
        "image": (f"{ingredient_id}.webp", image_bytes, "image/webp")
    }
    data = {
        "ingredientId": ingredient_id
    }

    res = client.post("/script/upload-ingredient-image", data=data, files=files)
    if res.status_code == 200:
        return True
    else:
        print(f"Upload error for {ingredient_id} ({res.status_code}): {res.text}")
        return False


def main():
    print("Starting Pinula Local AI Image Generator (SDXL-Lightning)")

    try:
        ingredients = load_json_file("dictionaries/ingredients.json")
        processed_ids = load_processed_ids()
        client = get_authenticated_client()
        pipe = initialize_sd_pipeline()
    except Exception as e:
        print(f"Initialization error: {e}")
        return

    to_process = [
        ing for ing in ingredients
        if str(ing.get("id") or ing.get("Id")) not in processed_ids
    ]
    total = len(to_process)

    print(f"Total ingredients: {len(ingredients)} | Processed: {len(processed_ids)} | Remaining: {total}")

    if not to_process:
        print("All ingredient images have been generated.")
        return

    for idx, item in enumerate(to_process, start=1):
        item_id = str(item.get("id") or item.get("Id"))
        item_name = extract_ingredient_name(item)

        print(f"[{idx}/{total}] Generating image for: '{item_name}' ({item_id})...")

        try:
            img_bytes = generate_image(pipe, item_name)
            success = upload_image(client, item_id, img_bytes)

            if success:
                save_processed_id(item_id)
                print(f"Successfully processed '{item_name}'.")
            else:
                print(f"Skipping checkpoint for '{item_name}' due to upload failure.")

        except Exception as e:
            print(f"Failed to generate/upload image for '{item_name}': {e}")

    print("\nImage generation completed.")


if __name__ == "__main__":
    main()