using System;
using System.Collections.Generic;

namespace EF;

public partial class Student
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string? Name { get; set; }

    public string? Surname { get; set; }

    public int GroupId { get; set; }

    public virtual FormsParticipation? FormsParticipation { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual ICollection<Subject> Subjects { get; } = new List<Subject>();
}
