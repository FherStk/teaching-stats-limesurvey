using System;
using System.Collections.Generic;

namespace DBContext;

public partial class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int DegreeId { get; set; }

    public virtual Degree Degree { get; set; } = null!;

    public virtual ICollection<FormsEvaluation> FormsEvaluations { get; } = new List<FormsEvaluation>();

    public virtual ICollection<Student> Students { get; } = new List<Student>();

    public virtual ICollection<SubjectTrainerGroup> SubjectTrainerGroups { get; } = new List<SubjectTrainerGroup>();
}
