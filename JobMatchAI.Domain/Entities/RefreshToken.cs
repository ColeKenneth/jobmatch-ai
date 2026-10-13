using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("refresh_tokens")]
    public class RefreshToken
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [Column("token", TypeName = "varchar(500)")]
        public string Token { get; set; } = string.Empty;

        [Column("expires_at", TypeName = "timestamp")]
        public DateTime ExpiresAt { get; set; }

        [Column("created_at", TypeName = "timestamp")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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
