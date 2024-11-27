// PhoneShop/shared/DTOs/LoginResult.cs
namespace Shared.DTOs
{
    public class LoginResult
    {
        public string Token { get; set; }
        public DateTime? Expiration { get; set; }
    }
}