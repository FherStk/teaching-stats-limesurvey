using System;
using System.Collections.Generic;

namespace EF;

public partial class Type
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Question> Questions { get; } = new List<Question>();
}
