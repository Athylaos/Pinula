using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pinula.Shared.Interface;
using Pinula.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Pinula.Shared;



namespace Pinula.ViewModel
{
    public partial class TestViewModel : ObservableObject
    {
        private IUserService _userService;
        private ICategoryService _categoryService;
        private IRecipeService _recipeService;


        public bool IsLoggedIn;
        [ObservableProperty] private string userName;


        public TestViewModel(IUserService userService, ICategoryService categoryService, IRecipeService recipeService)
        {
            _userService = userService;
            _categoryService = categoryService;
            _recipeService = recipeService;
        }

        public async void OnAppStartAsync()
        {
            IsLoggedIn = await _userService.IsUserLoggedInAsync();


            if(IsLoggedIn)
            {
                var user = await _userService.GetCurrentUserAsync();
                if(user == null)
                {
                    UserName = "Not logged in user null";
                }
                else
                {
                    UserName = $"Logged in {user.Name} email {user.Email}";

                }

            }
            else
            {
                UserName = "Not logged in";
            }
        }

        [RelayCommand]
        public async void LoginBtn()
        {
            Debug.WriteLine("LoginBtn");
            Shell.Current.GoToAsync(nameof(LoginPage));

        }

        [RelayCommand]
        public async Task DebugDbBtn()
        {
            /*
            var users = await _database.Table<UserDbModel>().ToListAsync();
            foreach (var u in users)
            {
                System.Diagnostics.Debug.WriteLine($"ID: {u.Id},  Name: '{u.Name}',Email: '{u.Email}', Hash: {u.PasswordHash}");
            }
            */
        }

        [RelayCommand]
        public async Task CategoryDbBtn()
        {
            /*
            var categories = await _database.Table<CategoryDbModel>().ToListAsync();
            foreach(var c in categories)
            {
                System.Diagnostics.Debug.WriteLine($"ID: {c.Id}, Name: '{c.Name}', Image: {c.PictureUrl}");
            }
            */
        }

        [RelayCommand]
        public async Task RecipeDbBtn()
        {
            /*
            var recipes = await _database.Table<RecipeDbModel>().ToListAsync();
            var recipesDb = await _recipeService.GetRecipesAsync(-1);
            foreach (var r in recipes)
            {
                System.Diagnostics.Debug.WriteLine($"ID: {r.Id}, Name: '{r.Title}', Image: {r.PhotoPath}");
            }
            foreach (var r in recipesDb)
            {
                System.Diagnostics.Debug.WriteLine($"From service ID: {r.Id}, Name: '{r.Title}', Image: {r.PhotoPath}");
            }
            */
        }


        [RelayCommand]
        public void RecipesMainPageBtn()
        {
            Shell.Current.GoToAsync("//RecipesMainPage");
        }

        [RelayCommand]
        public async Task RecipeDetailPageBtn()
        {
            await Shell.Current.GoToAsync(nameof(RecipeDetailsPage));
        }

        [RelayCommand]
        public async Task LogoutBtn()
        {
            _userService.Logout();
        }
    }
}
