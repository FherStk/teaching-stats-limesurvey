using System;
using System.Collections.Generic;

namespace EF;

public partial class DjangoContentType
{
    public int Id { get; set; }

    public string AppLabel { get; set; } = null!;

    public string Model { get; set; } = null!;

    public virtual ICollection<AuthPermission> AuthPermissions { get; } = new List<AuthPermission>();

    public virtual ICollection<DjangoAdminLog> DjangoAdminLogs { get; } = new List<DjangoAdminLog>();
}
