using Bunit;
using LogisticsApp.Frontend.Pages;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;
using static LogisticsApp.Frontend.Pages.UserManagement; // To access UserDto

namespace LogisticsApp.Tests
{
    public class UserManagementTests : TestContext
    {
        [Fact]
        public void ClickingOnboardButton_ShowsAddUserForm()
        {
            // --- 1. ARRANGE ---
            // Set up our fake HTTP client to intercept the API call made during OnInitializedAsync
            var mockHttp = new MockHttpMessageHandler();

            // Tell the mock: "When the component asks for /api/user, return an empty list."
            mockHttp.When("http://localhost/api/user")
        .Respond("application/json", "[]");

            // Inject the fake HttpClient into bUnit's services
            var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new Uri("http://localhost");
            Services.AddSingleton(httpClient);

            // Render the component
            var cut = Render<UserManagement>();

            // --- 2. ACT ---
            // Find the "Onboard Employee" button using standard CSS selectors and click it
            var onboardButton = cut.Find("button.btn-primary-action");
            onboardButton.Click();

            // --- 3. ASSERT ---
            // Verify that the HTML has updated to include the form header
            Assert.Contains("✨ New Employee Setup", cut.Markup);

            // Verify that the specific input field for the Full Name exists on the screen
            var nameInput = cut.Find("input[placeholder='e.g. Jane Doe']");
            Assert.NotNull(nameInput);
        }
    }
}