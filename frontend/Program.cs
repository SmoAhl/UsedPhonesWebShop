using Blazored.LocalStorage;
using frontend.Authentication;
using frontend.Handlers;
using frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using frontend;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register CustomAuthenticationStateProvider
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthenticationStateProvider>());

// Register AuthService
builder.Services.AddScoped<AuthService>();

// Register TokenAuthorizationHandler
builder.Services.AddTransient<TokenAuthorizationHandler>();

// Configure HttpClient with TokenAuthorizationHandler
builder.Services.AddHttpClient("BackendAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5088/"); // Changed to HTTP
})
.AddHttpMessageHandler<TokenAuthorizationHandler>();

// Register a default HttpClient that uses the named client
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("BackendAPI"));

// Add Authorization Core
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
