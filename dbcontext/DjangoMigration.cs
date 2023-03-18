using System;
using System.Collections.Generic;

namespace EF;

public partial class DjangoMigration
{
    public int Id { get; set; }

    public string App { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime Applied { get; set; }
}
