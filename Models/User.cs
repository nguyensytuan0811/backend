using System.ComponentModel.DataAnnotations;

namespace WebFashion.Models
{
    public enum UserRole
    {
        Admin = 1,
        User = 2
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public ICollection<RefreshToken>  RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}