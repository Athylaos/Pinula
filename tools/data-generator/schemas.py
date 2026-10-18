from typing import List, Optional
from uuid import UUID
from pydantic import BaseModel, Field


class IngredientUnitUpdateDto(BaseModel):
    unit_id: UUID = Field(
        description="GUID of the additional unit from the provided units list"
    )
    amount_in_grams: float = Field(
        description="Average weight in grams for 1 unit of this ingredient"
    )


class DGIngredientUpdateDto(BaseModel):
    id: UUID = Field(
        description="GUID of the ingredient matching the input ingredient ID"
    )
    default_unit_id: UUID = Field(
        description="GUID of the default base unit (most used one for this ingredient), from the provided units list"
    )
    shopping_category_id: UUID = Field(
        description="GUID of the shopping category from the provided categories list"
    )

    is_vegan: bool = Field(description="Whether the ingredient is vegan")
    is_vegetarian: bool = Field(description="Whether the ingredient is vegetarian")
    is_gluten_free: bool = Field(description="Whether the ingredient is gluten-free")
    is_lactose_free: bool = Field(description="Whether the ingredient is lactose-free")

    image_search_term: str = Field(
        description="Clean English search query for photo lookup (e.g. 'fresh whole milk', 'olive oil bottle')"
    )

    image_url: str = Field(
        default="default_ingredient.png",
        description="Default image filename or provided image name",
    )

    nutri_score: Optional[str] = Field(
        default=None,
        description="NutriScore grade ('A', 'B', 'C', 'D', or 'E')",
    )
    nova_classification: Optional[int] = Field(
        default=None,
        description="NOVA classification grade (1, 2, 3, or 4)",
    )

    additional_units: List[IngredientUnitUpdateDto] = Field(
        default_factory=list,
        description="List of logical additional units for this ingredient",
    )


class DGIngredientBatchResponse(BaseModel):
    items: List[DGIngredientUpdateDto] = Field(
        description="List of processed ingredients for the current batch"
    )