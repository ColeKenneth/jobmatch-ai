using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("refresh_tokens")]
    public class RefreshToken
    {
        [Key]
        [Column("id")]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; init; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; init; }

        [Column("token", TypeName = "varchar(500)")]
        public string Token { get; init; } = string.Empty;

        [Column("expires_at", TypeName = "timestamp")]
        public DateTime ExpiresAt { get; init; }

        [Column("created_at", TypeName = "timestamp")]
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        [Column("revoked_at", TypeName = "timestamp")]
        public DateTime? RevokedAt { get; set; }

        [Column("replaced_by_token", TypeName = "varchar(500)")]
        public string? ReplacedByToken { get; set; }

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        [NotMapped]
        public bool IsRevoked => RevokedAt != null;

        [NotMapped]
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
