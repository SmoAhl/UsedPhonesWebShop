namespace Shared.DTOs
{
    public class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Password { get; set; } // New password, if any
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
    }
}