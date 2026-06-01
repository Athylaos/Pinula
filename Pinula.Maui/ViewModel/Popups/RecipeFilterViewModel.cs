using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pinula.ModelsUI;
using Pinula.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Pinula.ViewModel.Popups
{
    public partial class RecipeFilterViewModel : ObservableObject
    {
        private TaskCompletionSource<RecipeFilterParameters?> _resultSource = new();
        public Task<RecipeFilterParameters?> Result => _resultSource.Task;

        [ObservableProperty]
        private RecipeFilterParameters filterParametrs = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayTime))]
        double timeSliderValue;
        private readonly int[] _timeSteps = { 1, 3, 5, 10, 20, 30, 40, 50, 60, 120 };
        private int time;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayCalories))]
        double caloriesSliderValue;
        private readonly int[] _caloriesSteps = { 1, 20, 50, 75, 100, 125, 150, 200, 250, 300, 350, 400, 450, 500, 600, 700, 800, 900, 1000, 1250, 1500, 1750, 2000};
        private int calories;

        [ObservableProperty]
        ObservableCollection<RatingStar> ratingStars = new ObservableCollection<RatingStar>
        {
            new RatingStar { RatingValue = 1, Icon = "favorite.png" },
            new RatingStar { RatingValue = 2, Icon = "favorite.png" },
            new RatingStar { RatingValue = 3, Icon = "favorite.png" },
            new RatingStar { RatingValue = 4, Icon = "favorite.png" },
            new RatingStar { RatingValue = 5, Icon = "favorite.png" }
        };

        public ObservableCollection<DifficultySelectable> Difficulties { get; set; } = new()
        {
            new DifficultySelectable { Difficulty = DifficultyLevel.Easy },
            new DifficultySelectable { Difficulty = DifficultyLevel.Medium },
            new DifficultySelectable { Difficulty = DifficultyLevel.Hard },
            new DifficultySelectable { Difficulty = DifficultyLevel.Chef }
        };

        [ObservableProperty]
        bool onlyFavorite;
        int currentRating = -1;

        partial void OnOnlyFavoriteChanged(bool value)
        {
            if(FilterParametrs is not null)
            {
                FilterParametrs.OnlyFavorites = value;
            }
        }

        [ObservableProperty]
        bool cookingTimeOn = false;
        [ObservableProperty]
        bool caloriesOn = false;
        [ObservableProperty]
        bool ratingOn = false;
        [ObservableProperty]
        bool difficultyOn = false;


        public string DisplayTime
        {
            get
            {
                int index = (int)Math.Round(TimeSliderValue);
                int minutes = _timeSteps[index];
                time = minutes;

                if (minutes < 60) return $"{minutes}m";

                int hours = minutes / 60;
                int remainingMins = minutes % 60;
                return remainingMins == 0 ? $"{hours}h" : $"{hours}h {remainingMins}m";
            }
        }

        public string DisplayCalories
        {
            get
            {
                int index = (int)Math.Round(CaloriesSliderValue);
                calories = _caloriesSteps[index];
                return $"{calories}kcal";
            }
        }


        public RecipeFilterViewModel()
        {

        }

        partial void OnFilterParametrsChanged(RecipeFilterParameters value)
        {
            if (value == null) return;

            if (value.MaxCookingTime > 0)
            {
                CookingTimeOn = true;
                TimeSliderValue = Array.IndexOf(_timeSteps, value.MaxCookingTime.Value);
                if (TimeSliderValue < 0) TimeSliderValue = 0;
            }
            if (value.MaxCalories > 0)
            {
                CaloriesOn = true;
                CaloriesSliderValue = Array.IndexOf(_caloriesSteps, value.MaxCalories.Value);
                if (CaloriesSliderValue < 0) CaloriesSliderValue = 0;
            }
            if (value.MinRating > 0)
            {
                SelectRating(value.MinRating.Value);
            }
            if (value.MaxDifficulty.HasValue)
            {
                var diff = Difficulties.FirstOrDefault(d => (int)d.Difficulty == value.MaxDifficulty);
                if (diff != null) diff.IsSelected = true;
            }
            OnlyFavorite = value.OnlyFavorites;
        }


        [RelayCommand]
        public void ApplyFilters()
        {
            FilterParametrs ??= new();
            if (CookingTimeOn)
            {
                FilterParametrs.MaxCookingTime = time;
            }
            else
            {
                FilterParametrs.MaxCookingTime = null;
            }

            if (CaloriesOn)
            {
                FilterParametrs.MaxCalories = calories;
            }
            else
            {
                FilterParametrs.MaxCalories = null;
            }

            if (RatingOn)
            {
                FilterParametrs.MinRating = currentRating;
            }
            else
            {
                FilterParametrs.MinRating = null;
            }

            var selectedDifficulty = Difficulties.FirstOrDefault(d => d.IsSelected);
            if (DifficultyOn && selectedDifficulty != null)
            {
                FilterParametrs.MaxDifficulty = (int)selectedDifficulty.Difficulty;
            }
            else
            {
                FilterParametrs.MaxDifficulty = null;
            }

            FilterParametrs.OnlyFavorites = OnlyFavorite;

            if(!CookingTimeOn && !CaloriesOn && !RatingOn && selectedDifficulty is null && !OnlyFavorite)
            {
                _resultSource.TrySetResult(null);
            }
            _resultSource.TrySetResult(FilterParametrs);
        }

        [RelayCommand]
        public void Cancel()
        {
            _resultSource.TrySetResult(null);
        }

        [RelayCommand]
        private void SelectRating(int rating)
        {
            RatingOn = true;
            currentRating = rating;
            for (int i = 1; i <= 5; i++)
            {
                RatingStars[i - 1].Icon = i <= rating ? "favorite_full.png" : "favorite.png";
            }
        }

        [RelayCommand]
        private void ClearRating()
        {
            SelectRating(0);
            RatingOn = false;
        }

        [RelayCommand]
        private void DifficultyBtn(DifficultySelectable selectedItem)
        {
            if (selectedItem == null) return;

            if (selectedItem.IsSelected)
            {
                selectedItem.IsSelected = false;
                DifficultyOn = false;
                return;
            }

            foreach (var d in Difficulties)
            {
                d.IsSelected = false;
            }

            selectedItem.IsSelected = true;
            DifficultyOn = true;
        }
    }
}
