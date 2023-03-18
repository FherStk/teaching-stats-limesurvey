using System;
using System.Collections.Generic;

namespace EF;

public partial class Degree
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int DepartmentId { get; set; }

    public int? LevelId { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<Group> Groups { get; } = new List<Group>();

    public virtual Level? Level { get; set; }

    public virtual ICollection<Subject> Subjects { get; } = new List<Subject>();
}
