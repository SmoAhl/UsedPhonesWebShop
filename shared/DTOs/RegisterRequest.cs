namespace Shared.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Role { get; set; }
        public string Password { get; set; } // Plain password input
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
    }
}