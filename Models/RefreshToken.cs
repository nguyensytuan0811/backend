using System.ComponentModel.DataAnnotations;

namespace WebFashion.Models;

public class RefreshToken
{  
    [Key]
    public int TokenId { get; set; }

    public int? UserId { get; set; }

    public string? Token { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? Revoked { get; set; }

    public virtual User? User { get; set; }
}