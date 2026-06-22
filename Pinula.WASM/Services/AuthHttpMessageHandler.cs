using Pinula.Shared.Interface;

using System.Net.Http.Headers;

namespace Pinula.WASM.Services
{
    public class AuthHttpMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorage _localStorage;

        public AuthHttpMessageHandler(ILocalStorage localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var culture = await _localStorage.GetStringAsync("culture");
            var cultureCode = string.IsNullOrEmpty(culture) ? "en" : culture;
            request.Headers.AcceptLanguage.Clear();
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(cultureCode));


            return await base.SendAsync(request, cancellationToken);


        }

    }
}
