using System;
using System.Collections.Generic;

namespace EF;

public partial class FormsEvaluation
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public int GroupId { get; set; }

    public int SubjectId { get; set; }

    public int? TrainerId { get; set; }

    public virtual ICollection<FormsAnswer> FormsAnswers { get; } = new List<FormsAnswer>();

    public virtual Group Group { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;

    public virtual Trainer? Trainer { get; set; }
}
