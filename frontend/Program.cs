using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using frontend;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add the main application component App. This component is rendered to the HTML element with id="app".
builder.RootComponents.Add<App>("#app");

// Add the HeadOutlet component to the <head> element. This allows dynamic content addition to the head section of the Blazor app.
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add HttpClient service to enable the application to make HTTP requests to the backend.
// The BaseAddress setting defines the base URL used for HTTP requests.
// This allows relative URLs like "/api/auth/register" or "/api/auth/login".
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5088") });

// Register Blazored.LocalStorage service
builder.Services.AddBlazoredLocalStorage();

// Build and run the Blazor WebAssembly application
await builder.Build().RunAsync();
