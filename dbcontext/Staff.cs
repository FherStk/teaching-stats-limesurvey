using System;
using System.Collections.Generic;

namespace DBContext;

public partial class Staff
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string? Name { get; set; }

    public string? Surname { get; set; }

    public string? Position { get; set; }
}
