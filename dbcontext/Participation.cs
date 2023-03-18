using System;
using System.Collections.Generic;

namespace DBContext;

public partial class Participation
{
    public DateTime? Timestamp { get; set; }

    public string? Email { get; set; }

    public string? Surname { get; set; }

    public string? Name { get; set; }

    public string? GroupName { get; set; }

    public string? DegreeName { get; set; }

    public string? LevelName { get; set; }

    public string? DepartmentName { get; set; }
}
