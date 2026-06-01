using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public partial class ProfileViewModel : ObservableObject
    {
        IUserService _userService;
        IRecipeService _recipeService;

        private FileResult? _selectedPhoto;
        private UserDisplayDto user = new();

        [ObservableProperty]
        ImageSource selectedImageSource;

        [ObservableProperty]
        bool isLoading = false;

        [ObservableProperty]
        string name;
        [ObservableProperty]
        bool nameEditable = false;

        [ObservableProperty]
        string surname;
        [ObservableProperty]
        bool surnameEditable = false;

        [ObservableProperty]
        string email;

        [ObservableProperty]
        bool saveChangesVisible;

        [ObservableProperty]
        int postedRecipes;
        [ObservableProperty]
        int postedComments;
        [ObservableProperty]
        decimal avgRating;
        [ObservableProperty]
        DateOnly userCreated;

        public ObservableCollection<RecipePreviewDto> MyOwnRecipes { get; set; } = new();
        [ObservableProperty]
        bool myOwnVisible;
        private readonly RecipeFilterParameters _myOwnFilter = new RecipeFilterParameters()
        {
            OnlyMine = true,
            Amount = 4
        };
        private const int _RecipeLoadAmount = 4;
        [ObservableProperty]
        bool loadingRecipes;


        public ProfileViewModel(IUserService userService, IRecipeService recipeService){
            _userService = userService;
            _recipeService = recipeService;

        }

        public async Task StartAsync()
        {
            IsLoading = true;

            var loadedUser = await _userService.GetCurrentUserAsync();

            if(loadedUser is null || string.IsNullOrWhiteSpace(loadedUser.Name))
            {
                Shell.Current.GoToAsync(nameof(LoginPage));
            }
            user = loadedUser??new();


            Name = user.Name;
            Surname = user.Surname;
            SelectedImageSource = user.AvatarUrl;

            SaveChangesVisible = false;
            NameEditable = false;
            SurnameEditable = false;

            PostedRecipes = user.PostedRecipes;
            PostedComments = user.PostedComments;
            AvgRating = user.AvgRating;
            UserCreated = DateOnly.FromDateTime(user.UserCreated);

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


            IsLoading = false;
        }

        [RelayCommand]
        public async Task LoadMoreMyOwnAsync() => await LoadMoreRecipesAsync(_myOwnFilter, MyOwnRecipes);

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
        public async Task SaveChangesBtn()
        {
            if (Name?.Length < 15 && Surname?.Length < 21 && Name?.Length > 2 && Surname?.Length > 2)
            {
                IsLoading = true;

                var updateDto = new UserUpdateDto
                {
                    Name = Name.Trim(),
                    Surname = Surname.Trim()
                };

                bool response;
                if(_selectedPhoto is not null)
                {
                    var photo = await _selectedPhoto.OpenReadAsync();
                    response = await _userService.UpdateUserAsync(updateDto, photo, _selectedPhoto.FileName, _selectedPhoto.ContentType);
                }
                else
                {
                    response = await _userService.UpdateUserAsync(updateDto, null, null, null);
                }



                if (response)
                {
                    await Shell.Current.DisplayAlertAsync("Success", "Profile was updated", "OK");
                    NameEditable = false;
                    SurnameEditable = false;
                    SaveChangesVisible = false;
                    _selectedPhoto = null;
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Error", "Profile update was unsuccesfull", "OK");
                    Name = user.Name;
                    Surname = user.Surname;
                    SelectedImageSource = user.AvatarUrl;

                    NameEditable = false;
                    SurnameEditable = false;
                    SaveChangesVisible = false;
                    _selectedPhoto = null;
                }

                IsLoading = false;
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Fail", "Name (3-14) or surname (3-20) doesn't have correct lenght.", "OK");
            }

        }

        [RelayCommand]
        public async Task DiscardChangesBtn()
        {
            Name = user.Name;
            Surname = user.Surname;
            SelectedImageSource = user.AvatarUrl;

            NameEditable = false;
            SurnameEditable = false;
            SaveChangesVisible = false;
        }

        [RelayCommand]
        public void EditSurnameBtn()
        {
            SurnameEditable = true;
            NameEditable = true;
            SaveChangesVisible = true;
        }

        [RelayCommand]
        public void EditNameBtn()
        {
            NameEditable = true;
            SaveChangesVisible = true;
        }

        [RelayCommand]
        public async Task LogoutBtn()
        {
            _userService.Logout();
            Shell.Current.GoToAsync(nameof(LoginPage));
        }

        [RelayCommand]
        public async Task ChangeProfilePictureBtn()
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

            SaveChangesVisible = true;
        }

        [RelayCommand]
        public void RecipesMainPageBtn()
        {
            Shell.Current.GoToAsync("//RecipesMainPage");
        }

        [RelayCommand]
        public void ProfilePageBtn()
        {
            Shell.Current.GoToAsync(nameof(ProfilePage));
        }


        [RelayCommand]
        public async Task GoBackBtn()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
