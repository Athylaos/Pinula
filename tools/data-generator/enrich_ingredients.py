import json
import os
import time
from typing import List, Set
import httpx
from dotenv import load_dotenv
from google import genai
from google.genai import types

from schemas import DGIngredientBatchResponse

load_dotenv()

GEMINI_API_KEY = os.getenv("GEMINI_API_KEY")
PINULA_API_URL = os.getenv("PINULA_API_URL", "https://localhost:7123")
ADMIN_EMAIL = os.getenv("ADMIN_EMAIL")
ADMIN_PASSWORD = os.getenv("ADMIN_PASSWORD")

CHECKPOINT_FILE = "processed_ids.txt"
BATCH_SIZE = 20
MAX_RETRIES = 3


def validate_environment():
    missing = []
    if not GEMINI_API_KEY:
        missing.append("GEMINI_API_KEY")
    if not ADMIN_EMAIL:
        missing.append("ADMIN_EMAIL")
    if not ADMIN_PASSWORD:
        missing.append("ADMIN_PASSWORD")
    if missing:
        raise ValueError(f"Missing environment variables: {', '.join(missing)}")


def load_json_file(file_path: str):
    if not os.path.exists(file_path):
        raise FileNotFoundError(f"Dictionary file not found: {file_path}")
    with open(file_path, "r", encoding="utf-8") as f:
        return json.load(f)


def load_processed_ids() -> Set[str]:
    if not os.path.exists(CHECKPOINT_FILE):
        return set()
    with open(CHECKPOINT_FILE, "r", encoding="utf-8") as f:
        return set(line.strip() for line in f if line.strip())


def save_processed_ids(ids: List[str]):
    with open(CHECKPOINT_FILE, "a", encoding="utf-8") as f:
        for item_id in ids:
            f.write(f"{item_id}\n")


def get_authenticated_client() -> httpx.Client:
    client = httpx.Client(base_url=PINULA_API_URL, verify=False, timeout=30.0)

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


def process_batch_with_gemini(
        gemini_client: genai.Client,
        batch_ingredients: List[dict],
        units_json: List[dict],
        categories_json: List[dict]
) -> List[dict]:
    prompt = f"""
You are an expert nutritionist and culinary specialist.
Enrich the provided list of ingredients with accurate metadata and unit conversions.

INSTRUCTIONS:
1. Retain the exact original "id" for each ingredient.
2. Select the best "shopping_category_id" GUID from the provided CATEGORIES list.
3. Select the best "default_unit_id" GUID from the provided UNITS list (typically Gram or Milliliter, or other most relevant unit for this ingredient).
4. Add logical additional units to "additional_units" (e.g. tablespoon, cup, piece, clove) with accurate "amount_in_grams".
5. Set dietary flags correctly (is_vegan, is_vegetarian, is_gluten_free, is_lactose_free).
6. Estimate NutriScore ('A' to 'E') and NOVA classification (1 to 4).
7. Generate a clean English "image_search_term" suitable for photo prompts (e.g. "fresh whole milk", "olive oil bottle").

AVAILABLE UNITS:
{json.dumps(units_json, ensure_ascii=False, indent=2)}

AVAILABLE SHOPPING CATEGORIES:
{json.dumps(categories_json, ensure_ascii=False, indent=2)}

INGREDIENTS TO PROCESS:
{json.dumps(batch_ingredients, ensure_ascii=False, indent=2)}
"""

    response = gemini_client.models.generate_content(
        model="gemini-3.5-flash-lite",
        contents=prompt,
        config=types.GenerateContentConfig(
            response_mime_type="application/json",
            response_schema=DGIngredientBatchResponse,
            temperature=0.2,
        ),
    )

    parsed_data = DGIngredientBatchResponse.model_validate_json(response.text)

    payload = []
    for item in parsed_data.items:
        payload.append({
            "Id": str(item.id),
            "DefaultUnitId": str(item.default_unit_id),
            "ShoppingCategoryId": str(item.shopping_category_id),
            "IsVegan": item.is_vegan,
            "IsVegetarian": item.is_vegetarian,
            "IsGlutenFree": item.is_gluten_free,
            "IsLactoseFree": item.is_lactose_free,
            "ImageUrl": "default_ingredient.png",
            "NutriScore": item.nutri_score,
            "NovaClassification": item.nova_classification,
            "AdditionalUnits": [
                {
                    "UnitId": str(u.unit_id),
                    "AmountInGrams": u.amount_in_grams
                }
                for u in item.additional_units
            ]
        })
    return payload


def main():
    print("Starting Pinula Ingredient AI Enricher...")

    try:
        validate_environment()
        ingredients = load_json_file("dictionaries/ingredients.json")
        units = load_json_file("dictionaries/units.json")
        categories = load_json_file("dictionaries/shopping_categories.json")
    except Exception as e:
        print(f"Initialization error: {e}")
        return

    processed_ids = load_processed_ids()
    to_process = [ing for ing in ingredients if str(ing.get("id")) not in processed_ids]

    print(f"Total ingredients: {len(ingredients)} | Processed: {len(processed_ids)} | Remaining: {len(to_process)}")

    if not to_process:
        print("All ingredients have already been processed.")
        return

    try:
        gemini_client = genai.Client(api_key=GEMINI_API_KEY)
        http_client = get_authenticated_client()
    except Exception as e:
        print(f"Setup error: {e}")
        return

    total_batches = (len(to_process) + BATCH_SIZE - 1) // BATCH_SIZE

    for i in range(0, len(to_process), BATCH_SIZE):
        batch = to_process[i:i + BATCH_SIZE]
        current_batch_num = (i // BATCH_SIZE) + 1
        print(f"\nProcessing batch {current_batch_num}/{total_batches} ({len(batch)} items)...")

        payload = None
        for attempt in range(1, MAX_RETRIES + 1):
            try:
                payload = process_batch_with_gemini(gemini_client, batch, units, categories)
                break
            except Exception as e:
                print(f"Gemini API attempt {attempt}/{MAX_RETRIES} failed: {e}")
                if attempt < MAX_RETRIES:
                    time.sleep(5)
                else:
                    print("Max retries reached for Gemini API. Stopping execution.")
                    return

        try:
            res = http_client.put("/script/batch-update-ingredients-ai", json=payload)
            if res.status_code == 200:
                batch_ids = [str(ing["id"]) for ing in batch]
                save_processed_ids(batch_ids)
                print(f"Batch {current_batch_num} saved successfully.")
            else:
                print(f"API Update Error ({res.status_code}): {res.text}")
                break
        except Exception as e:
            print(f"HTTP request error: {e}")
            break

        time.sleep(4.5)

    print("\nProcessing completed.")


if __name__ == "__main__":
    main()