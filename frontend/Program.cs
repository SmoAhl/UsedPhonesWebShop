using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using frontend;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Lisää sovelluksen pääkomponentti App. Tämä komponentti renderöidään HTML-sivulle kohteessa, jossa on id="app".
builder.RootComponents.Add<App>("#app");

// Lisää HeadOutlet-komponentti <head>-elementtiin. Tämä mahdollistaa Blazor-sovelluksen dynaamisen sisältöjen lisäyksen head-osaan.
builder.RootComponents.Add<HeadOutlet>("head::after");

// Lisää HttpClient-palvelu, jotta sovellus voi tehdä HTTP-pyyntöjä backendille.
// BaseAddress-asetuksella määritellään sovelluksen perusosoite, jota käytetään HTTP-pyyntöjen yhteydessä.
// Tällöin URL-osoite voi olla suhteellinen, kuten "/api/auth/register" tai "/api/auth/login".
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5088") });

// Rakentaa ja käynnistää Blazor WebAssembly -sovelluksen
await builder.Build().RunAsync();
