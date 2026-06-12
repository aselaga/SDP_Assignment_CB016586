using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace LogisticsApp.Frontend.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;

        // ✅ No HttpClient — token attachment is handled by JwtAuthorizationHandler
        public CustomAuthStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsStringAsync("authToken");

                if (string.IsNullOrWhiteSpace(token))
                    return Anonymous();

                token = token.Trim('"');

                if (token.Split('.').Length != 3)
                    return Anonymous();

                if (IsTokenExpired(token))
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    return Anonymous();
                }

                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "jwt");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Auth] Failed: {ex.Message}");
                return Anonymous();
            }
        }

        public void NotifyUserLoggedIn(string token)
        {
            try
            {
                var claims = ParseClaimsFromJwt(token);
                var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
            }
            catch
            {
                NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
            }
        }

        public void NotifyUserLoggedOut()
        {
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
        }

        private static AuthenticationState Anonymous()
            => new(new ClaimsPrincipal(new ClaimsIdentity()));

        private static bool IsTokenExpired(string jwt)
        {
            try
            {
                var payload = jwt.Split('.')[1];
                var jsonBytes = ParseBase64WithoutPadding(payload);
                var keyValuePairs = JsonSerializer
                    .Deserialize<Dictionary<string, JsonElement>>(jsonBytes);

                if (keyValuePairs != null &&
                    keyValuePairs.TryGetValue("exp", out var expElement))
                {
                    var expiry = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64());
                    return expiry.UtcDateTime < DateTime.UtcNow;
                }
            }
            catch { }

            return false;
        }

        // ✅ Checks all three role key formats backends commonly use
        private const string ClrRoleClaimType =
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer
                .Deserialize<Dictionary<string, JsonElement>>(jsonBytes);

            if (keyValuePairs == null) return claims;

            // ✅ Handle role in any format the backend might send
            foreach (var roleKey in new[] { "role", ClaimTypes.Role, ClrRoleClaimType })
            {
                if (!keyValuePairs.TryGetValue(roleKey, out var rolesElement)) continue;

                if (rolesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in rolesElement.EnumerateArray())
                        claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? ""));
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, rolesElement.GetString() ?? ""));
                }

                keyValuePairs.Remove(roleKey);
                break;
            }

            // ✅ Map all remaining claims safely
            foreach (var kvp in keyValuePairs)
            {
                var value = kvp.Value.ValueKind == JsonValueKind.String
                    ? kvp.Value.GetString() ?? ""
                    : kvp.Value.ToString();

                claims.Add(new Claim(kvp.Key, value));
            }

            return claims;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            base64 = base64.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}