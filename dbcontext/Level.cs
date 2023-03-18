using System;
using System.Collections.Generic;

namespace DBContext;

public partial class Level
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public virtual ICollection<Degree> Degrees { get; } = new List<Degree>();

    public virtual ICollection<Question> Questions { get; } = new List<Question>();
}
