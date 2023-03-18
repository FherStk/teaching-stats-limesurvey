using System;
using System.Collections.Generic;

namespace DBContext;

public partial class FormsAnswer
{
    public int Id { get; set; }

    public string? Value { get; set; }

    public int QuestionId { get; set; }

    public int EvaluationId { get; set; }

    public virtual FormsEvaluation Evaluation { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
