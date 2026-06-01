using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pinula.Shared.Models;
using Pinula.Shared.DTOs;
using Pinula.Shared.Interface;
using Pinula.View;
using Pinula.ModelsUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace Pinula.ViewModel
{
    [QueryProperty(nameof(RecipeIdString), "RecipeIdString")]
    public partial class RecipeDetailsViewModel : ObservableObject
    {

        private IRecipeService _recipeService;
        private IUserService _userService;

        [ObservableProperty]
        string recipeIdString;
        [ObservableProperty]
        Guid recipeId;

        partial void OnRecipeIdStringChanged(string value)
        {
            if (Guid.TryParse(value, out var guid))
            {
                RecipeId = guid;
                _ = LoadRecipeAsync(guid);
            }
        }

        [ObservableProperty]
        private RecipeDetailsDto selectedRecipe = new();
        [ObservableProperty]
        private string favoriteIconPath = "favorite.png";
        [ObservableProperty]
        private bool isLoading = true;
        [ObservableProperty]
        private ObservableCollection<CommentPreview> visibleComments = new();

        [ObservableProperty]
        decimal recipeRating;
        [ObservableProperty]
        int recipeUsersRated;

        private int ratingValue;
        [ObservableProperty]
        private DateOnly commentTime;
        [ObservableProperty]
        private bool editorEditable = true;
        [ObservableProperty]
        private bool postBtnVisible = true;
        [ObservableProperty]
        private bool delGridVisible = false;
        [ObservableProperty]
        private string commentText = string.Empty;
        [ObservableProperty]
        private ObservableCollection<RatingStar> ratingStars = new ObservableCollection<RatingStar>
        {
            new RatingStar { RatingValue = 1, Icon = "favorite.png" },
            new RatingStar { RatingValue = 2, Icon = "favorite.png" },
            new RatingStar { RatingValue = 3, Icon = "favorite.png" },
            new RatingStar { RatingValue = 4, Icon = "favorite.png" },
            new RatingStar { RatingValue = 5, Icon = "favorite.png" }
        };


        public async Task LoadRecipeAsync(Guid id)
        {
            IsLoading = true;

            try
            {
                var result = await _recipeService.GetRecipeDetailsAsync(id);

                if (result == null)
                {
                    await Shell.Current.DisplayAlertAsync("Error", "Recipe failed to load", "OK");
                    return;
                }

                SelectedRecipe = result;

                RecipeRating = SelectedRecipe.Rating ?? (decimal)3.5;
                RecipeUsersRated = SelectedRecipe.UsersRated ?? 0;

                FavoriteIconPath = SelectedRecipe.IsFavorite ? "favorite_full.png" : "favorite.png";
                VisibleComments.Clear();
                foreach(var c in SelectedRecipe.Comments.Take(10))
                {
                    VisibleComments.Add(c);
                }

                await CommentStatus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recipe: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public RecipeDetailsViewModel(IRecipeService recipeService, IUserService userService)
        {
            _recipeService = recipeService;
            _userService = userService;
        }

        [RelayCommand]
        public void IngredientTappedCommand(RecipeIngredient ig)
        {
            if(ig is null) return;

            //ig.IsReady = !ig.IsReady;

        }

        [RelayCommand]
        public async Task FavoriteBtn()
        {

            if(!await _userService.IsUserLoggedInAsync())
            {
                Shell.Current.GoToAsync(nameof(LoginPage));
            }
            else
            {
                var user = await _userService.GetCurrentUserAsync();
                await _recipeService.ChangeFavoriteAsync(selectedRecipe.Id, user.Id);

                if(FavoriteIconPath == "favorite.png")
                {
                    FavoriteIconPath = "favorite_full.png";
                }
                else
                {
                    FavoriteIconPath = "favorite.png";
                }
            }
        }

        [RelayCommand]
        public async Task GoBackBtn()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async  Task PostComment()
        {
            if(!await _userService.IsUserLoggedInAsync())
            {
                Shell.Current.GoToAsync(nameof(LoginPage));
                return;
            }

            if (string.IsNullOrEmpty(CommentText))
            {
                CommentText = string.Empty;
            }

            Comment comment = new Comment()
            {
                RecipeId = recipeId,
                Text = CommentText,
                Rating = (short)ratingValue,
                CreatedAt = DateTime.Now,
                User = new()

            };

            var res = await _recipeService.PostCommentAsync(comment);

            if (res.NewAverageRating.HasValue )
            {
                RecipeRating = res.NewAverageRating ?? 0;
                RecipeUsersRated = res.NewUsersRatedCount ?? 0;
            }
            CommentTime = DateOnly.FromDateTime(res.NewComment.CreatedAt);

            comment.User.Name = res.NewComment.UserName;
            comment.User.Surname = res.NewComment.UserSurname;       

            PostBtnVisible = false;
            EditorEditable = false;
            DelGridVisible = true;
        }

        [RelayCommand]
        private void SelectRating(int selectedRating)
        {
            UpdateRatingStars(selectedRating);
        }

        private void UpdateRatingStars(int rating)
        {
            RatingStars.Clear();
            for (int i = 1; i <= 5; i++)
            {
                var star = new RatingStar
                {
                    RatingValue = i,
                    Icon = i <= rating ? "favorite_full.png" : "favorite.png"
                };
                RatingStars.Add(star);
            }
            ratingValue = rating;
        }

        private async Task CommentStatus()
        {
            var comment  = await _recipeService.GetRecipeCommentAsync(RecipeId, null);

            if(comment is not null)
            {
                CommentTime = DateOnly.FromDateTime(comment.NewComment.CreatedAt);
                CommentText = comment.NewComment.Text;
                SelectRating((int)comment.NewComment.Rating);
                PostBtnVisible = false;
                EditorEditable = false;
                DelGridVisible = true;
                return;
            }
            PostBtnVisible = true;
            EditorEditable = true;
            DelGridVisible = false;
            CommentText = "";
            SelectRating(1);
        }

        [RelayCommand]
        private async Task DeleteComment()
        {
            var res = await _recipeService.DeleteRecipeCommentAsync(SelectedRecipe.Id, null);
            if (res is null) return;
            SelectedRecipe.Rating = res.NewAverageRating;
            SelectedRecipe.UsersRated = res.NewUsersRatedCount;

            PostBtnVisible = true;
            EditorEditable = true;
            DelGridVisible = false;

            SelectRating(1);
            CommentText = "";
        }
    }

}

