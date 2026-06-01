using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pinula.Shared;
using Pinula.Shared.Interface;
using Pinula.Shared.Services;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using Pinula.View;
using Pinula.View.Popups;
using Pinula.ViewModel.Popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace Pinula.ViewModel
{
    public partial class RecipesMainViewModel : ObservableObject
    {
        private ICategoryService _categoryService;
        private IIngredientService _ingredientsService;
        private IRecipeService _recipesService;
        private IUserService _userService;

        private CancellationTokenSource? _searchCts;
        private const int _RecipeLoadAmount = 4;
        [ObservableProperty]
        bool loadingRecipes;

        [ObservableProperty]
        bool isSearching;
        [ObservableProperty]
        string searchTerm;
        [ObservableProperty]
        RecipeFilterParameters filterParametrs;
        [ObservableProperty]
        bool isEmpty;
        public ObservableCollection<RecipePreviewDto> SearchedRecipes { get; set; } = new();
        public ObservableCollection<Category> Categories { get; set; } = new();

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

        private readonly RecipeFilterParameters _favouriteFilter = new  RecipeFilterParameters()
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

        public RecipesMainViewModel(ICategoryService category, IIngredientService ingredient, IRecipeService recipe, IUserService user)
        {
            _categoryService = category;
            _ingredientsService = ingredient;
            _recipesService = recipe;
            _userService = user;
        }

        public async void StartAsync()
        {
            var cts = await _categoryService.GetAllCategoriesAsync();
            Categories.Clear();
            foreach (var ct in cts)
            {
                Categories.Add(ct);
            }

            await RefreshRecipesLists();

        }

        private async Task RefreshRecipesLists()
        {
            var fvRcps = await _recipesService.GetFilteredRecipePreviewsAsync(_favouriteFilter, null);
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

            var popRcps = await _recipesService.GetFilteredRecipePreviewsAsync(_popularFilter, null);
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


            var fstRcps = await _recipesService.GetFilteredRecipePreviewsAsync(_fastFilter, null);
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

            var myRcps = await _recipesService.GetFilteredRecipePreviewsAsync(_myOwnFilter, null);
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

                var newRecipes = await _recipesService.GetFilteredRecipePreviewsAsync(filter, null);

                if (newRecipes != null && newRecipes.Any())
                {
                    foreach (var recipe in newRecipes)
                    {
                        if(!list.Any(x=>x.Id == recipe.Id)) list.Add(recipe);
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
        public async Task LoadMorePopularAsync() => await LoadMoreRecipesAsync (_popularFilter, PopularRecipes);
        [RelayCommand]
        public async Task LoadMoreFastAsync() => await LoadMoreRecipesAsync(_fastFilter, FastRecipes);
        [RelayCommand]
        public async Task LoadMoreMyOwnAsync() => await LoadMoreRecipesAsync(_myOwnFilter, MyOwnRecipes);

        

        [RelayCommand]
        public async Task AddRecipeBtn()
        {
            Debug.WriteLine(_userService.IsUserLoggedInAsync());
            if (await _userService.IsUserLoggedInAsync())
            {
                await Shell.Current.GoToAsync(nameof(AddRecipePage));
            }
            else
            {               
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        [RelayCommand]
        public async Task CategoryBtn(Category category)
        {
            if (category == null) return;

            await Shell.Current.GoToAsync($"{nameof(RecipesCategoryPage)}?CategoryId={category.Id}",true);
        }

        [RelayCommand]
        public async Task RecipeBtn(RecipePreviewDto recipe)
        {
            if (recipe is null) return;

            await Shell.Current.GoToAsync($"{nameof(RecipeDetailsPage)}?RecipeIdString={recipe.Id}", true);
 
        }


        partial void OnSearchTermChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsSearching = false;
                return;
            }
            RestartSearch(true);
        }

        private void RestartSearch(bool isDebounced)
        {
            IsSearching = true;
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();

            _ = SearchAsync(_searchCts.Token, isDebounced);
        }

        [RelayCommand]
        public async Task OpenFilterPopup()
        {
            var vm = new RecipeFilterViewModel { FilterParametrs = FilterParametrs??new() };
            var popup = new RecipeFilterPopup(vm);
            Shell.Current.CurrentPage.ShowPopup(popup);

            var result = await vm.Result;
            if (result != null)
            {
                FilterParametrs = result;
                RestartSearch(false);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(SearchTerm))
                {
                    IsSearching = false;
                }
                else
                {
                    FilterParametrs = new();
                    RestartSearch(false);
                }
            }
        }

        [RelayCommand]
        public void ClearFilters()
        {
            SearchTerm = string.Empty;
            FilterParametrs = new RecipeFilterParameters();
            IsSearching = false;
            return;
        }

        private async Task SearchAsync(CancellationToken token, bool withDelay)
        {
            try
            {
                if (withDelay)
                    await Task.Delay(500, token);

                if (!IsSearching)
                {
                    MainThread.BeginInvokeOnMainThread(() => {
                        IsEmpty = false;
                        SearchedRecipes.Clear();
                    });
                    return;
                }

                if (FilterParametrs is null) FilterParametrs = new();
                FilterParametrs.SearchTerm = SearchTerm;

                var results = await _recipesService.GetFilteredRecipePreviewsAsync(FilterParametrs, token);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (token.IsCancellationRequested) return;

                    IsSearching = true;
                    SearchedRecipes.Clear();

                    foreach (var r in results)
                        SearchedRecipes.Add(r);

                    IsEmpty = SearchedRecipes.Count == 0;
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
        }

        [RelayCommand]
        public void DashboardPageBtn()
        {
            Shell.Current.GoToAsync("//DashboardPage");
        }
    }
}
