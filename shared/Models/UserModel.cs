namespace Shared.Models
{
    public class UserModel
    {
        public int UserID { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        // For frontend login
        public string? Password { get; set; } // Made nullable
        // For backend storage
        public string? PasswordHash { get; set; } // Made nullable
        // Additional Properties Required by Backend
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
