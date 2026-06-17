using Microsoft.JSInterop;
using Pinula.Shared.Interface;

namespace Pinula.WASM.Services
{
    public class BlazorLocalStorage : ILocalStorage
    {
        private readonly IJSRuntime _js;
        private const string TokenKey = "auth_token";

        public BlazorLocalStorage(IJSRuntime js)
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

        public async Task SetStringAsync(string key, string value)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", key, value);
        }

        public async Task<string?> GetStringAsync(string key)
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", key);
        }
    }
}

