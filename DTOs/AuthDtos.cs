namespace WebFashion.DTOs
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public int UserId { get; set; }
        public int UserRole { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        
        public string RefreshToken  {get;set;}
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateUserProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}