using System;

namespace UTC_DATN.Entities;

public partial class ApplicationView
{
    public Guid ViewId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid ViewerId { get; set; }
    public DateTime ViewedAt { get; set; }

    public virtual Application Application { get; set; }
    public virtual User Viewer { get; set; }
}
