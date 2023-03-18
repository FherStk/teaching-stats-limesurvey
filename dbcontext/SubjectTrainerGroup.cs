using System;
using System.Collections.Generic;

namespace EF;

public partial class SubjectTrainerGroup
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public int TrainerId { get; set; }

    public int? GroupId { get; set; }

    public virtual Group? Group { get; set; }

    public virtual Subject Subject { get; set; } = null!;

    public virtual Trainer Trainer { get; set; } = null!;
}
