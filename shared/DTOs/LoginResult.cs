// PhoneShop/shared/DTOs/LoginResult.cs
namespace Shared.DTOs
{
    public class LoginResult
    {
        public string Token { get; set; }
        public DateTime? Expiration { get; set; }
        public string Email { get; set; } // Lisätään sähköposti
        public string Role { get; set; }  // Lisätään rooli (Customer/Admin)
    }
}