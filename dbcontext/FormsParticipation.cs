using System;
using System.Collections.Generic;

namespace DBContext;

public partial class FormsParticipation
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public int StudentId { get; set; }

    public virtual Student Student { get; set; } = null!;
}
