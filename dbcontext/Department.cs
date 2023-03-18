using System;
using System.Collections.Generic;

namespace DBContext;

public partial class Department
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Degree> Degrees { get; } = new List<Degree>();
}
