using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pinula.Shared.Models;
using Pinula.Shared.DTOs;
using Pinula.Shared.Interface;
using Pinula.View;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;

namespace Pinula.ViewModel
{
    public partial class RegisterViewModel : ObservableObject
    {
        private IUserService _userService;

        [ObservableProperty] private string nameEntry;
        [ObservableProperty] private string surnameEntry;
        [ObservableProperty] private string emailEntry;

        [ObservableProperty] private string password1;
        [ObservableProperty] private string password2;

        [ObservableProperty] bool indicatorVisibility;
        [ObservableProperty] string? indicatorText;


        public RegisterViewModel(IUserService userService)
        {
            _userService = userService;

        }

        private bool IsValidEmail(string email)
        {
            if(string.IsNullOrEmpty(email)) return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch { return false; }
        }

        [RelayCommand]
        public async Task RegisterBtn()
        {
            if (string.IsNullOrWhiteSpace(NameEntry))
            {
                IndicatorVisibility = true;
                IndicatorText = "Name is mandatory";
                return;
            }
            IndicatorVisibility = false;
            if (string.IsNullOrWhiteSpace(SurnameEntry))
            {
                IndicatorVisibility = true;
                IndicatorText = "Surname is mandatory";
                return;
            }
            IndicatorVisibility = false;

            if (!IsValidEmail(EmailEntry) || string.IsNullOrWhiteSpace(EmailEntry))
            {
                IndicatorVisibility = true;
                IndicatorText = "Invalid email format";
                return;
            }
            IndicatorVisibility = false;

            if (Password1 != Password2)
            {
                IndicatorVisibility = true;
                IndicatorText = "Password doesn't match";
                return;
            }

            var registrationDto = new UserRegistrationDto()
            {
                Name = NameEntry,
                Email = EmailEntry,
                Surname = SurnameEntry,
                Password = Password1,
            };

            if (!await _userService.RegisterAsync(registrationDto))
            {
                IndicatorVisibility = true;
                IndicatorText = "This email is already registred, please login";
                return;
            }

            if (await _userService.LoginAsync(new UserLoginDto() { Email = EmailEntry, Password = Password1}) == null)
            {
                return;
            }

            Shell.Current.GoToAsync("//TestPage");

        }

        [RelayCommand]
        public void LoginBtn()
        {
            Shell.Current.GoToAsync(nameof(LoginPage));
        }

        [RelayCommand]
        public void BackBtn()
        {
            Shell.Current.Navigation.PopAsync(true);
        }

        [RelayCommand]
        public void SkipBtn()
        {
            Shell.Current.GoToAsync(nameof(TestPage));
        }

    }
}
