using System;
using System.Collections.Generic;

namespace EF;

public partial class Topic
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Question> Questions { get; } = new List<Question>();

    public virtual ICollection<Subject> Subjects { get; } = new List<Subject>();
}
