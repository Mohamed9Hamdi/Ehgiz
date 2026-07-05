using System.ComponentModel.DataAnnotations.Schema;

namespace Ehgiz.DAL.Entities;

public class PasswordResetCode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CodeHash { get; set; } = null!;
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;

    [NotMapped]
    public bool IsActive => UsedAt == null && DateTime.UtcNow < ExpiresAt;
}
