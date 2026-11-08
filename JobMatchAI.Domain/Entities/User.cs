using JobMatchAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Column("first_name", TypeName = "varchar(100)")]
        public string FirstName { get; set; } = string.Empty;

        [Column("middle_name", TypeName = "varchar(100)")]
        public string? MiddleName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;

        [Column("email", TypeName = "varchar(255)")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash", TypeName = "varchar(255)")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("role", TypeName = "varchar(20)")]
        public UserRole Role { get; set; }

        [Column("created_at", TypeName = "timestamp")]
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        [Column("updated_at", TypeName = "timestamp")]
        public DateTime? UpdatedAt { get; set; }

        [Column("is_active", TypeName = "bool")]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public string FullName => string.IsNullOrWhiteSpace(MiddleName) ?
            $"{FirstName} {LastName}" : $"{FirstName} {MiddleName} {LastName}";

        public Student? Student { get; init; }
        public Alumni? Alumni { get; init; }
        public Employer? Employer { get; init; }

            
    }
}
