using System;
using System.Collections.Generic;

namespace DBContext;

public partial class Trainer
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<FormsEvaluation> FormsEvaluations { get; } = new List<FormsEvaluation>();

    public virtual ICollection<SubjectTrainerGroup> SubjectTrainerGroups { get; } = new List<SubjectTrainerGroup>();
}
