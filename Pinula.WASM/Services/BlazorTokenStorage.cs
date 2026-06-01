using Microsoft.JSInterop;
using Pinula.Shared.Interface;

namespace Pinula.WASM.Services
{
    public class BlazorTokenStorage : ITokenStorage
    {
        private readonly IJSRuntime _js;
        private const string TokenKey = "auth_token";

        public BlazorTokenStorage(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SaveTokenAsync(string token)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }

        public async Task RemoveTokenAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        }
    }
}

