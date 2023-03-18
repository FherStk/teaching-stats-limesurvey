using System;
using System.Collections.Generic;

namespace DBContext;

public partial class AnswerCfMp
{
    public int? EvaluationId { get; set; }

    public DateTime? Timestamp { get; set; }

    public double? Year { get; set; }

    public string? Level { get; set; }

    public string? Department { get; set; }

    public string? Degree { get; set; }

    public string? Group { get; set; }

    public string? SubjectCode { get; set; }

    public string? SubjectName { get; set; }

    public string? Trainer { get; set; }

    public string? Topic { get; set; }

    public short? QuestionSort { get; set; }

    public string? QuestionType { get; set; }

    public string? QuestionStatement { get; set; }

    public string? Value { get; set; }
}
