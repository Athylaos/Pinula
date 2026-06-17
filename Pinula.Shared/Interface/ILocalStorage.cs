using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.Interface
{
    public interface ILocalStorage
    {
        Task SaveTokenAsync(string token);
        Task<string?> GetTokenAsync();
        Task RemoveTokenAsync();

        Task SetStringAsync(string key, string value);
        Task<string?> GetStringAsync(string key);
    }
}
