using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pinula.Shared.Models;
using Pinula.Shared;
using Pinula.Shared.Interface;
using Pinula.View.Popups;
using Pinula.ViewModel.Popups;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Pinula.Shared.DTOs;



namespace Pinula.ViewModel
{
    public partial class CategorySelectable : ObservableObject
    {
        public Category Category { get; set; }

        [ObservableProperty]
        private bool isSelected;

        public CategorySelectable(Category category, bool isSelected = false)
        {
            Category = category;
            IsSelected = isSelected;
        }
    }

    public partial class AddRecipeViewModel : ObservableObject
    {
        private readonly IIngredientService _ingredientsService;
        private readonly IRecipeService _recipesService;
        private readonly ICategoryService _categoryService;
        private readonly IUnitService _unitsService;

        [ObservableProperty]
        ImageSource selectedImageSource;

        [ObservableProperty] string title;
        [ObservableProperty] string time;
        [ObservableProperty] string servings;
        [ObservableProperty] string photoPath;
        public ObservableCollection<UnitPreviewDto> ServingUnits { get; } = new ObservableCollection<UnitPreviewDto>();
        [ObservableProperty] private UnitPreviewDto selectedServingUnit;
        public List<DifficultyLevel> DifficultyOptions { get; } = Enum.GetValues(typeof(DifficultyLevel)).Cast<DifficultyLevel>().ToList();
        [ObservableProperty] DifficultyLevel difficulty = DifficultyLevel.Medium;

        public ObservableCollection<RecipeIngredient> Ingredients { get; } = new();
        public ObservableCollection<CategorySelectable> Categories { get; set; } = new ObservableCollection<CategorySelectable>();
        public ObservableCollection<RecipeStep> RecipeSteps { get; } = new ObservableCollection<RecipeStep>();

        [ObservableProperty] string warningText;
        [ObservableProperty] bool warningEnabled;
        private bool _isInitialized = false;
        private FileResult? _selectedPhoto;




        public AddRecipeViewModel(IIngredientService ingredientsService, IRecipeService recipeService, ICategoryService categoryService, IUnitService unitService)
        {
            _ingredientsService = ingredientsService;
            _recipesService = recipeService;
            _categoryService = categoryService;
            _unitsService = unitService;
            AddCookingStepBtn();
            AddCookingStepBtn();
            SelectedImageSource = "default_recipe_picture.png";
        }

        public async Task StartAsync()
        {

            if (_isInitialized) return;

            var su = await _unitsService.GetAllServingUnitsAsync();
            ServingUnits.Clear();
            foreach (var s in su)
            {
                ServingUnits.Add(s);
            }
            SelectedServingUnit = ServingUnits.FirstOrDefault(su => su.Name == "portion") ?? ServingUnits.First();

            var ca = await _categoryService.GetAllCategoriesAsync();
            Categories.Clear();
            foreach(var c in ca)
            {
                var cs = new CategorySelectable(c);
                Categories.Add(cs);
            }
            _isInitialized = true;
           
        }

        [RelayCommand]
        public async Task AddPhotoBtn()
        {
            try
            {
                var result = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
                {
                    Title = "Choose a photo for the recipe",
                    SelectionLimit = 1

                });

                if (result != null && result.Count > 0)
                {
                    _selectedPhoto = result.First();
                    var stream = await _selectedPhoto.OpenReadAsync();
                    SelectedImageSource = ImageSource.FromStream(() => stream);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Unable to load photo: " + ex.Message, "OK");
            }
        }



        [RelayCommand]
        public void CategoryBtn(CategorySelectable category)
        {
            if(category != null)
            {
                category.IsSelected = !category.IsSelected;
            }

        }

        [RelayCommand]
        public async Task OpenAddIngredientPopup()
        {
            List<IngredientPreview> ingredients = await _ingredientsService.GetIngredientPreviewsAsync(4);

            var popupVm = new AddIngredientPopupViewModel(ingredients, _unitsService, _ingredientsService);

            popupVm.OnCloseRequest += (resultData) =>
            {
                if (resultData is RecipeIngredient newIngredient)
                {
                    Ingredients.Add(newIngredient);
                }
                OnPropertyChanged(nameof(Categories));
            };

            var popup = new AddIngredientPopup();
            popup.BindingContext = popupVm;

            await Shell.Current.ShowPopupAsync(popup);
        }

        [RelayCommand]
        public void DelIngredientBtn(RecipeIngredient ingredient)
        {
            Ingredients.Remove(ingredient);
        }

        [RelayCommand]
        public void AddCookingStepBtn()
        {
            RecipeSteps.Add(new RecipeStep { StepNumber = (short)(RecipeSteps.Count+1)});
        }

        [RelayCommand]
        public void RemoveCookingStepBtn()
        {
            if(RecipeSteps.Count > 1)
            {
                RecipeSteps.RemoveAt(RecipeSteps.Count - 1);
            }
        }

        [RelayCommand]
        public async Task GoBackBtn()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task FinishRecipeBtn()
        {
            int timeInt = int.TryParse(Time, out var q) ? q : 0;
            int servingsInt = int.TryParse(Servings, out var p) ? p : 0;

            if (string.IsNullOrWhiteSpace(Title))
            {
                WarningEnabled = true;
                WarningText = "Title is mandatory";
                return;
            }
            if (string.IsNullOrWhiteSpace(Time) || q == 0)
            {
                WarningEnabled = true;
                WarningText = "Time is mandatory and can't be 0";
                return;
            }
            if (string.IsNullOrWhiteSpace(Servings) || p == 0)
            {
                WarningEnabled = true;
                WarningText = "Number of servings is mandatory and can't be 0";
                return;
            }
            if (!Categories.Any(c => c.IsSelected))
            {
                WarningEnabled = true;
                WarningText = "Recipe must have at least one category";
                return;
            }
            if(Ingredients.Count == 0)
            {
                WarningEnabled = true;
                WarningText = "Recipe must have at least one ingredient";
                return;
            }
            foreach(var ing in Ingredients)
            {
                if(ing.Quantity == 0)
                {
                    WarningEnabled = true;
                    WarningText = $"Ingredient can't has zero quantity ({ing.Ingredient.Name})";
                    return;
                }
                if (ing.Unit == null)
                {
                    WarningEnabled = true;
                    WarningText = $"Ingredient must have unit ({ing.Ingredient.Name})";
                    return;
                }
            }

            foreach(var rs in RecipeSteps)
            {
                if (string.IsNullOrEmpty(rs.Description))
                {
                    WarningEnabled = true;
                    WarningText = $"Cooking step can't be empty ({rs.StepNumber})";
                    return;
                }
            }

            RecipeCreateDto rc = new RecipeCreateDto
            {
                Title = Title,
                CookingTime = (short)timeInt,
                ServingsAmount = (short)servingsInt,
                ServingUnit = SelectedServingUnit.Id,
                Difficulty = (short)Difficulty,
                CategoriesIds = Categories.Where(c => c.IsSelected).Select(c => c.Category.Id).ToList(),
                RecipeIngredients = Ingredients.ToList(),
                RecipeSteps = RecipeSteps.ToList(),

            };

            if(_selectedPhoto is not null)
            {
                var photo = await _selectedPhoto.OpenReadAsync();

                await _recipesService.SaveRecipeAsync(rc, photo, _selectedPhoto.FileName, _selectedPhoto.ContentType);
            }
            else
            {
                await _recipesService.SaveRecipeAsync(rc, null, null, null);
            }


            Title = string.Empty;
            Time = string.Empty;
            Servings = string.Empty;
            Difficulty = DifficultyLevel.Medium;
            foreach (var cat in Categories) cat.IsSelected = false;
            Ingredients.Clear();
            RecipeSteps.Clear();


            await Shell.Current.Navigation.PopAsync(true);
        }

    }
}
