using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTC_DATN.Entities
{
    public class Notification
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } // e.g., "JOB_OPPORTUNITY", "APPLICATION_UPDATE", "SECURITY", "MARKETING"

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string RelatedId { get; set; } // e.g., JobId or ApplicationId for navigation
    }
}
