#nullable disable
using System;

namespace UTC_DATN.Entities;

public class CoverLetter
{
    public Guid CoverLetterId { get; set; }
    public Guid CandidateId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Candidate Candidate { get; set; }
}
