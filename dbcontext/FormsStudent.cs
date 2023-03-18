using System;
using System.Collections.Generic;

namespace EF;

public partial class FormsStudent
{
    public int? Id { get; set; }

    public string? Email { get; set; }

    public string? Name { get; set; }

    public string? Surname { get; set; }

    public int? LevelId { get; set; }

    public string? LevelCode { get; set; }

    public string? LevelName { get; set; }

    public int? GroupId { get; set; }

    public string? GroupName { get; set; }

    public int? DegreeId { get; set; }

    public string? DegreeCode { get; set; }

    public string? Subjects { get; set; }
}
