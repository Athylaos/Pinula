using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pinula.Shared.Models;
using Pinula.Shared.DTOs;
using Pinula.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Pinula.Shared.Interface;

namespace Pinula.ViewModel.Popups
{
    public partial class IngredientUnitOption : ObservableObject
    {
        [ObservableProperty]
        private UnitPreviewDto selectedUnit;

        [ObservableProperty]
        private decimal? conversionFactor;
    }
    public partial class AddIngredientPopupViewModel : ObservableObject
    {

        private List<IngredientPreview> _allIngredientsSource;
        private CancellationTokenSource _warningTokenSource;

        [ObservableProperty]
        bool createIngredientMode;
        [ObservableProperty]
        bool addIngredientMode;

        public ObservableCollection<IngredientPreview> FilteredIngredients {  get; set; } = new ObservableCollection<IngredientPreview>();
        [ObservableProperty] string searchText;

        [ObservableProperty] IngredientPreview selectedIngredient;
        [ObservableProperty] float quantity;

        [ObservableProperty] UnitPreviewDto allUnits;

        public event Action<object> OnCloseRequest;


        public ObservableCollection<UnitPreviewDto> UnitPreviews { get; set; } = new ObservableCollection<UnitPreviewDto>();

        [ObservableProperty]
        string ingredientName;
        [ObservableProperty]
        UnitPreviewDto selectedDefaultUnit = new();
        [ObservableProperty]
        IngredientUnitOption selectedDefaultOption = new();
        [ObservableProperty]
        bool preferdUnitVisibility = false;
        public ObservableCollection<IngredientUnitOption> AditionalUnits { get; set; } = new ObservableCollection<IngredientUnitOption>();

        [ObservableProperty]
        string warningText;
        [ObservableProperty]
        bool warningVisibility;

        [ObservableProperty]
        decimal? calories;
        [ObservableProperty]
        decimal? proteins;
        [ObservableProperty]
        decimal? fats;
        [ObservableProperty]
        decimal? carbohydrates;
        [ObservableProperty]
        decimal? fiber;

        private readonly IUnitService _unitService;
        private readonly IIngredientService _ingredientService;

        public AddIngredientPopupViewModel(List<IngredientPreview> il, IUnitService unitservice, IIngredientService ingredientService)
        {
            _allIngredientsSource = il;
            OnSearchTextChanged(string.Empty);

            _unitService = unitservice;
            _ingredientService = ingredientService;

            _ = StartAsync();

            AddIngredientMode = true;
            CreateIngredientMode = false;
        }

        public async Task StartAsync()
        {
            var u = await _unitService.GetAllUnitsAsync();

            foreach (var unit in u)
            {
                UnitPreviews.Add(unit);
            }

        }

        partial void OnSelectedDefaultUnitChanged(UnitPreviewDto value)
        {
            if (value == null) return;

            if(value.Name != "g")
            {
                SelectedDefaultOption.SelectedUnit = value;
                SelectedDefaultOption.ConversionFactor = null;
                PreferdUnitVisibility = true;
            }
            else
            {
                SelectedDefaultOption.SelectedUnit = value;
                SelectedDefaultOption.ConversionFactor = 1;
                PreferdUnitVisibility = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredIngredients.Clear();
            if(string.IsNullOrEmpty(value))
            {
                foreach(var i in _allIngredientsSource.Take(10))
                {
                    FilteredIngredients.Add(i);
                }
            }
            else
            {
                var filtered = _allIngredientsSource.Where(i => i.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
                foreach(var i in filtered.Take(10))
                {
                    FilteredIngredients.Add(i);
                }
            }
        }
        [RelayCommand]
        public Task Confirm()
        {
            if (SelectedIngredient is null) return Task.CompletedTask;
            if (SelectedIngredient?.SelectedUnit is null) return Task.CompletedTask;

            var result = new RecipeIngredient
            {
                Ingredient = new Ingredient
                {
                    Id = SelectedIngredient.Id,
                    Name = SelectedIngredient.Name
                },

                Unit = new Unit
                {
                    Id = SelectedIngredient.SelectedUnit.Id,
                    Name = SelectedIngredient.SelectedUnit.Name
                },

                Quantity = (decimal)Quantity,
                UnitId = SelectedIngredient.SelectedUnit.Id,
                IngredientId = SelectedIngredient.Id
            };

            OnCloseRequest?.Invoke(result);
            return Task.CompletedTask;
        }

        [RelayCommand]
        public Task Cancel()
        {
            OnCloseRequest?.Invoke(null);
            return Task.CompletedTask;
        }

        [RelayCommand]
        public void AddAditionalUnit()
        {
            AditionalUnits.Add(new IngredientUnitOption());
        }

        [RelayCommand]
        public void RemoveAditionalUnit(IngredientUnitOption option)
        {
            if (option is null) return;

            AditionalUnits.Remove(option);
            return;
        }

        [RelayCommand]
        public void ToggleModes()
        {
            CreateIngredientMode = !CreateIngredientMode;
            AddIngredientMode = !AddIngredientMode;
        }

        [RelayCommand]
        public async Task CreateIngredient()
        {

            if (string.IsNullOrWhiteSpace(IngredientName))
            {
                await ShowWarningAsync("Name of ingredient is mandatory");
                return;

            }

            if (SelectedDefaultOption.SelectedUnit is null)
            {
                await ShowWarningAsync("Default unit is mandatory");
                return;
            }

            if(SelectedDefaultOption.ConversionFactor is null || SelectedDefaultOption.ConversionFactor == 0)
            {
                await ShowWarningAsync("Conversion for default unit must be filled and can't be 0");
                return;
            }

            if(Calories is null || Calories < 0)
            {
                await ShowWarningAsync("Calories must be filled and can't be negative");
                return;
            }
            if (Proteins is null || Proteins < 0)
            {
                await ShowWarningAsync("Proteins must be filled and can't be negative");
                return;
            }
            if (Fats is null || Fats < 0)
            {
                await ShowWarningAsync("Fats must be filled and can't be negative");
                return;
            }
            if (Carbohydrates is null || Carbohydrates < 0)
            {
                await ShowWarningAsync("Carbohydrates must be filled and can't be negative");
                return;
            }
            if (Fiber is null || Fiber < 0)
            {
                await ShowWarningAsync("Fiber must be filled and can't be negative");
                return;
            }
            foreach (var (au, index) in AditionalUnits.Select((value, i) => (value, i)))
            {
                if (au.SelectedUnit is null)
                {
                    await ShowWarningAsync($"Unit at row {index + 1} must be selected");
                    return;
                }

                if (au.ConversionFactor is null or <= 0)
                {
                    await ShowWarningAsync($"Unit {au.SelectedUnit.Name} must have a valid conversion factor");
                    return;
          
                }
            }
            if(SelectedDefaultOption.SelectedUnit.Name == "g")
            {
                AditionalUnits.Add(new IngredientUnitOption { SelectedUnit = UnitPreviews.FirstOrDefault(up => up.Name == "g"), ConversionFactor = 1 });
            }
            else
            {
                AditionalUnits.Add(new IngredientUnitOption { SelectedUnit = UnitPreviews.FirstOrDefault(up => up.Name == "g"), ConversionFactor = 1 });
                AditionalUnits.Add(new IngredientUnitOption { SelectedUnit = SelectedDefaultOption.SelectedUnit, ConversionFactor = SelectedDefaultOption.ConversionFactor });
            }

            IngredientCreateDto dto = new IngredientCreateDto
            {
                Name = IngredientName,
                DefaultUnitId = SelectedDefaultOption.SelectedUnit.Id,
                Calories = Calories ?? 0,
                Proteins = Proteins ?? 0,
                Fats = Fats ?? 0,
                Carbohydrates = Carbohydrates ?? 0,
                Fiber = Fiber ?? 0,
                AdditionalUnits = AditionalUnits.Select(au => new CreateIngredientUnitDto
                {
                    UnitId = au.SelectedUnit.Id,
                    ToDefaultUnit = au.ConversionFactor ?? 1
                }).ToList()
            };

            var result = await _ingredientService.CreateIngredientAsync(dto);

            if (result.IsSuccess)
            {
                ToggleModes();

            }
            else
            {
                await ShowWarningAsync(result.Message);
            }
        }


        private async Task ShowWarningAsync(string message)
        {
            _warningTokenSource?.Cancel();
            _warningTokenSource = new CancellationTokenSource();
            var token = _warningTokenSource.Token;

            WarningText = message;
            WarningVisibility = true;

            try
            {
                await Task.Delay(3000, token);
                WarningVisibility = false;
            }
            catch (TaskCanceledException)
            {
            }
        }

    }
}
