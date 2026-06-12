using Blazored.LocalStorage;
using LogisticsApp.Frontend.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LogisticsApp.Frontend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // ✅ Use JwtAuthorizationHandler — avoids conflict with ASP.NET's AuthorizationHandler<T>
            builder.Services.AddTransient<JwtAuthorizationHandler>();

            builder.Services.AddHttpClient("AuthAPI", client =>
                client.BaseAddress = new Uri("http://localhost:5008/"))
                .AddHttpMessageHandler<JwtAuthorizationHandler>();

            builder.Services.AddScoped(sp =>
                sp.GetRequiredService<IHttpClientFactory>()
                  .CreateClient("AuthAPI"));

            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

            await builder.Build().RunAsync();
        }
    }
}