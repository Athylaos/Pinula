using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pinula.Shared;
using Pinula.Shared.Interface;
using Pinula.View;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.ViewModel
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IUserService _userService;

        [ObservableProperty] string? email;
        [ObservableProperty] string? password;

        [ObservableProperty] bool indicatorVisibility;
        [ObservableProperty] string? indicatorText;


        public LoginViewModel(IUserService userService)
        {
            _userService = userService;
            IndicatorVisibility = false;
            Email = string.Empty;
            Password = string.Empty;


        }

        public LoginViewModel()
        {
            Email = string.Empty;
            Password = string.Empty;
        }



        [RelayCommand]
        public async Task LoginBtn()
        {
            /*
            if (!await _userService.IsEmailRegistredAsync(Email))
            {
                IndicatorVisibility = true;
                IndicatorText = "Email not registered, please register";
                return;
            }
            */

            if(await _userService.LoginAsync(new UserLoginDto() { Email = Email, Password = Password}) == null)
            {
                IndicatorVisibility = true;
                IndicatorText = "Wrong email or password";
                return;
            }

            Shell.Current.GoToAsync("//TestPage");
            System.Diagnostics.Debug.WriteLine("Prihlaseno");

        }

        [RelayCommand]
        public void CreateAccountBtn()
        {
            Shell.Current.GoToAsync(nameof(RegisterPage));
        }

        [RelayCommand]
        public void ForgotPasswordBtn()
        {
            IndicatorVisibility = true;
            IndicatorText = "For password reset please contact support";

        }

        [RelayCommand]
        public void SkipBtn()
        {
            System.Diagnostics.Debug.WriteLine("SkipBtn");
            Shell.Current.Navigation.PopAsync(true);
        }



    }
}
