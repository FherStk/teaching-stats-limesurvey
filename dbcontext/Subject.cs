using System;
using System.Collections.Generic;

namespace DBContext;

public partial class Subject
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int DegreeId { get; set; }

    public int? TopicId { get; set; }

    public virtual Degree Degree { get; set; } = null!;

    public virtual ICollection<FormsEvaluation> FormsEvaluations { get; } = new List<FormsEvaluation>();

    public virtual ICollection<SubjectTrainerGroup> SubjectTrainerGroups { get; } = new List<SubjectTrainerGroup>();

    public virtual Topic? Topic { get; set; }

    public virtual ICollection<Student> Students { get; } = new List<Student>();
}
