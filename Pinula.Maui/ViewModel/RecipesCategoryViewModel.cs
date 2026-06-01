using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pinula.Shared;
using Pinula.Shared.Interface;
using Pinula.Shared.Services;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using Pinula.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace Pinula.ViewModel
{
    [QueryProperty(nameof(CategoryId), "CategoryId")]
    public partial class RecipesCategoryViewModel : ObservableObject
    {
        private readonly ICategoryService _categoryService;
        private readonly IRecipeService _recipeService;

        public RecipesCategoryViewModel(ICategoryService categoryService, IRecipeService recipeService)
        {
            _categoryService = categoryService;
            _recipeService = recipeService;
        }

        [ObservableProperty]
        private string categoryId;

        private Guid _categoryIdGuid;

        partial void OnCategoryIdChanged(string value)
        {
            if (Guid.TryParse(value, out var guid))
            {
                _categoryIdGuid = guid;
                _ = LoadCategoryAsync(guid);
            }
        }

        [ObservableProperty]
        bool isLoading;

        [ObservableProperty]
        Category selectedCategory;

        [ObservableProperty]
        private ObservableCollection<RecipePreviewDto> favoriteRecipes = new ObservableCollection<RecipePreviewDto>();

        private const int _RecipeLoadAmount = 4;
        [ObservableProperty]
        bool loadingRecipes;

        #region RecipeCollections
        public ObservableCollection<RecipePreviewDto> FavouriteRecipes { get; set; } = new();
        public ObservableCollection<RecipePreviewDto> PopularRecipes { get; set; } = new();
        public ObservableCollection<RecipePreviewDto> FastRecipes { get; set; } = new();
        public ObservableCollection<RecipePreviewDto> MyOwnRecipes { get; set; } = new();

        [ObservableProperty]
        bool favouriteVisible;
        [ObservableProperty]
        bool popularVisible;
        [ObservableProperty]
        bool fastVisible;
        [ObservableProperty]
        bool myOwnVisible;

        private readonly RecipeFilterParameters _favouriteFilter = new RecipeFilterParameters()
        {
            OnlyFavorites = true,
            Amount = 4
        };
        private readonly RecipeFilterParameters _popularFilter = new RecipeFilterParameters()
        {
            MinRating = 4,
            Amount = 4
        };
        private readonly RecipeFilterParameters _fastFilter = new RecipeFilterParameters()
        {
            MaxCookingTime = 20,
            Amount = 4
        };
        private readonly RecipeFilterParameters _myOwnFilter = new RecipeFilterParameters()
        {
            OnlyMine = true,
            Amount = 4
        };

        #endregion

        private async Task RefreshRecipesLists()
        {
            var fvRcps = await _recipeService.GetFilteredRecipePreviewsAsync(_favouriteFilter, null);
            FavouriteRecipes.Clear();
            if (fvRcps is null || fvRcps.Count == 0)
            {
                FavouriteVisible = false;
            }
            else
            {
                FavouriteVisible = true;
                foreach (var r in fvRcps)
                {
                    FavouriteRecipes.Add(r);
                }
            }

            var popRcps = await _recipeService.GetFilteredRecipePreviewsAsync(_popularFilter, null);
            PopularRecipes.Clear();
            if (popRcps is null || popRcps.Count == 0)
            {
                PopularVisible = false;
            }
            else
            {
                PopularVisible = true;
                foreach (var r in popRcps)
                {
                    PopularRecipes.Add(r);
                }
            }


            var fstRcps = await _recipeService.GetFilteredRecipePreviewsAsync(_fastFilter, null);
            FastRecipes.Clear();
            if (fstRcps is null || fstRcps.Count == 0)
            {
                FastVisible = false;
            }
            else
            {
                FastVisible = true;
                foreach (var r in fstRcps)
                {
                    FastRecipes.Add(r);
                }
            }

            var myRcps = await _recipeService.GetFilteredRecipePreviewsAsync(_myOwnFilter, null);
            MyOwnRecipes.Clear();
            if (myRcps is null || myRcps.Count == 0)
            {
                MyOwnVisible = false;
            }
            else
            {
                MyOwnVisible = true;
                foreach (var r in myRcps)
                {
                    MyOwnRecipes.Add(r);
                }
            }

        }

        private async Task LoadMoreRecipesAsync(RecipeFilterParameters filterPar, ObservableCollection<RecipePreviewDto> list)
        {
            if (LoadingRecipes) return;

            try
            {
                LoadingRecipes = true;
                var skip = list.Count;
                var filter = filterPar.Clone();
                filter.Skip = skip;
                filter.Amount = _RecipeLoadAmount;

                var newRecipes = await _recipeService.GetFilteredRecipePreviewsAsync(filter, null);

                if (newRecipes != null && newRecipes.Any())
                {
                    foreach (var recipe in newRecipes)
                    {
                        if (!list.Any(x => x.Id == recipe.Id)) list.Add(recipe);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Loading error: {ex.Message}");
            }
            finally
            {
                LoadingRecipes = false;
            }
        }

        [RelayCommand]
        public async Task LoadMoreFavouriteAsync() => await LoadMoreRecipesAsync(_favouriteFilter, FavouriteRecipes);
        [RelayCommand]
        public async Task LoadMorePopularAsync() => await LoadMoreRecipesAsync(_popularFilter, PopularRecipes);
        [RelayCommand]
        public async Task LoadMoreFastAsync() => await LoadMoreRecipesAsync(_fastFilter, FastRecipes);
        [RelayCommand]
        public async Task LoadMoreMyOwnAsync() => await LoadMoreRecipesAsync(_myOwnFilter, MyOwnRecipes);

        public async Task LoadCategoryAsync(Guid id)
        {
            IsLoading = true;
            SelectedCategory = await _categoryService.GetCategoryByIdAsync(id) ?? new();

            _favouriteFilter.CategoryId = id;
            _popularFilter.CategoryId = id;
            _fastFilter.CategoryId = id;
            _myOwnFilter.CategoryId = id;

            await RefreshRecipesLists();
            IsLoading = false;
        }

        [RelayCommand]
        public async Task RecipeBtn(RecipePreviewDto recipe)
        {
            if (recipe == null) return;

            await Shell.Current.GoToAsync($"{nameof(RecipeDetailsPage)}?RecipeIdString={recipe.Id}", true);

        }

        [RelayCommand]
        public void RecipesMainPageBtn()
        {
            Shell.Current.GoToAsync("//RecipesMainPage");
        }

        [RelayCommand]
        public void DashboardPageBtn()
        {
            Shell.Current.GoToAsync("//DashboardPage");
        }
    }
}
