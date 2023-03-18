using System;
using System.Collections.Generic;

namespace EF;

public partial class Question
{
    public int Id { get; set; }

    public short Sort { get; set; }

    public string Statement { get; set; } = null!;

    public DateTime? Disabled { get; set; }

    public int TypeId { get; set; }

    public int LevelId { get; set; }

    public int TopicId { get; set; }

    public DateTime Created { get; set; }

    public virtual ICollection<FormsAnswer> FormsAnswers { get; } = new List<FormsAnswer>();

    public virtual Level Level { get; set; } = null!;

    public virtual Topic Topic { get; set; } = null!;

    public virtual Type Type { get; set; } = null!;
}
