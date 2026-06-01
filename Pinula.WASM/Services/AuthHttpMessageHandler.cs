using Pinula.Shared.Interface;

using System.Net.Http.Headers;

namespace Pinula.WASM.Services
{
    public class AuthHttpMessageHandler : DelegatingHandler
    {
        private readonly ITokenStorage _tokenStorage;

        public AuthHttpMessageHandler(ITokenStorage tokenStorage)
        {
            _tokenStorage = tokenStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenStorage.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);


        }

    }
}
