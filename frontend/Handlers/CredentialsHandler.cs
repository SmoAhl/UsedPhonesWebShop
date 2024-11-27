// PhoneShop/frontend/Handlers/CredentialsHandler.cs
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace frontend.Handlers
{
    public class CredentialsHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Ensure credentials (cookies) are included
            request.Headers.Add("X-Credentials-Mode", "include");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}