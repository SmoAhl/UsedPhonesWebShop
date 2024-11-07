using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Backend.Data;
using Backend.Api;
using Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Swagger-dokumentaatio
// Lisätään Swagger-tuki, joka mahdollistaa API-dokumentaation luomisen
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Used Phones API", Version = "v1" });
});

// Lisää CORS-käytäntö, joka sallii kaikki originit, metodit ja otsikot
// (Voit myöhemmin rajoittaa tämän tiettyihin arvoihin tietoturvan parantamiseksi.)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("http://localhost:5058") // Salli pyynnöt Blazor-frontendistä
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Lisää Newtonsoft.Json tuki
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    });

var app = builder.Build();

// Käytä CORS-käytäntöä ennen API-reittien rekisteröintiä
app.UseCors("AllowBlazorClient");

// Swagger-käyttöliittymä
// Käytetään Swaggerin käyttöliittymää, jotta API-päätepisteet ovat helppokäyttöisiä ja testattavissa selaimessa
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Määritellään Swagger-dokumentaation sijainti ja URL-reitti
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Used Phones API V1");
    c.RoutePrefix = string.Empty; // Määrittää, että Swagger UI avautuu suoraan root-URL:ssa
});

// Tietokannan alustaminen asynkronisesti
await DatabaseInitializer.Initialize();

// Rekisteröi Phones API -päätepisteet
app.MapPhonesApi();

// Rekisteröidään AuthApi-päätepisteet
app.MapAuthApi(); // Rekisteröidään rekisteröinti- ja kirjautumispäätepisteet sovellukseen

// Käynnistetään sovellus
app.Run();
