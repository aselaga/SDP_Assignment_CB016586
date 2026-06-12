using Blazored.LocalStorage;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace LogisticsApp.Frontend.Auth
{
    public class JwtAuthorizationHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        public JwtAuthorizationHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");

            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Trim('"'));

            return await base.SendAsync(request, cancellationToken);
        }
    }
}