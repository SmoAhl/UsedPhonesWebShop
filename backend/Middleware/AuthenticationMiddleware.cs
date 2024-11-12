using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Backend.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        // Middleware-luokan konstruktori, joka ottaa vastaan seuraavan middleware-käsittelijän
        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // InvokeAsync-metodi, joka käsittelee jokaisen saapuvan HTTP-pyynnön
        public async Task InvokeAsync(HttpContext context)
        {
            // Tallenna pyynnön polku muuttujaan
            var path = context.Request.Path;

            // Tarkistetaan, onko pyyntö suunnattu API-päätepisteeseen, mutta ei autentikointiin liittyviin päätepisteisiin (/api/auth)
            if (path.StartsWithSegments("/api") && !path.StartsWithSegments("/api/auth"))
            {
                // Jos käyttäjällä ei ole istuntotietoja (UserID tai SessionID puuttuu), palautetaan 401 Unauthorized
                if (context.Session.GetInt32("UserID") == null || context.Session.GetString("SessionID") == null)
                {
                    // Määritetään vastauksen statuskoodiksi 401 Unauthorized
                    context.Response.StatusCode = 401; // Unauthorized

                    // Kirjoitetaan vastausviesti asiakkaalle
                    await context.Response.WriteAsync("Unauthorized: Kirjaudu sisään jatkaaksesi.");
                    return; // Lopetetaan pyyntö tähän, eikä jatketa seuraavaan middlewareen
                }

                // Tarkistetaan käyttäjän rooli
                var userRole = context.Session.GetString("Role");
                if (userRole == "customer")
                {
                    // Jos käyttäjän rooli on "customer", estetään pääsy auth-päätepisteisiin
                    context.Response.StatusCode = 403; // Forbidden
                    await context.Response.WriteAsync("Forbidden: Sinulla ei ole oikeuksia tähän resurssiin.");
                    return; // Lopetetaan pyyntö tähän, eikä jatketa seuraavaan middlewareen
                }
            }

            // Jos käyttäjä on autentikoitu tai pyyntö ei vaadi autentikointia, siirrytään seuraavaan middleware-komponenttiin
            await _next(context);
        }
    }
}
