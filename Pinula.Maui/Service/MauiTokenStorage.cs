using Pinula.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Maui.Service
{
    public class MauiTokenStorage : ITokenStorage
    {
        private const string TokenKey = "auth_token";

        public async Task SaveTokenAsync(string token)
        {
            await SecureStorage.Default.SetAsync(TokenKey, token);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(TokenKey);
        }

        public async Task RemoveTokenAsync()
        {
            SecureStorage.Default.Remove(TokenKey);
        }

    }
}
