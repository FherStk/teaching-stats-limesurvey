using System;
using System.Collections.Generic;

namespace EF;

public partial class FormsSubject
{
    public int? Id { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public int? DegreeId { get; set; }

    public string? DegreeCode { get; set; }

    public string? DegreeName { get; set; }

    public int? TrainerId { get; set; }

    public int? GroupId { get; set; }
}
