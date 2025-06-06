using System.ComponentModel.DataAnnotations;

namespace AppEL.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public decimal Balance { get; set; } = 0;
    }

    public enum UserRole
    {
        Admin,
        Instructor,
        Student
    }
}
