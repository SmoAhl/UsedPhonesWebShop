// PhoneShop/frontend/Authentication/CustomAuthenticationStateProvider.cs
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Blazored.LocalStorage;
using System.Threading.Tasks;
using System.Linq;

namespace frontend.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;

        public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            var identity = new ClaimsIdentity();

            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                identity = new ClaimsIdentity(jwtToken.Claims.Distinct().Select(c =>
                    c.Type == "nameid" ? new Claim(ClaimTypes.NameIdentifier, c.Value) :
                    c.Type == "email" ? new Claim(ClaimTypes.Email, c.Value) :
                    c.Type == "role" ? new Claim(ClaimTypes.Role, c.Value) :
                    c
                ).ToList(), "Bearer");

                Console.WriteLine("Token Claims:");
                foreach (var claim in jwtToken.Claims)
                {
                    Console.WriteLine($"{claim.Type}: {claim.Value}");
                }

                // Add roles only once
                var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").Distinct();
                foreach (var claim in roleClaims)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
                }
            }

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public void MarkUserAsAuthenticated(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claims = jwtToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "Bearer");
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void MarkUserAsLoggedOut()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }
    }
}