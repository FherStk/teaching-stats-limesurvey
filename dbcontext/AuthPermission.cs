using System;
using System.Collections.Generic;

namespace EF;

public partial class AuthPermission
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int ContentTypeId { get; set; }

    public string Codename { get; set; } = null!;

    public virtual ICollection<AuthGroupPermission> AuthGroupPermissions { get; } = new List<AuthGroupPermission>();

    public virtual ICollection<AuthUserUserPermission> AuthUserUserPermissions { get; } = new List<AuthUserUserPermission>();

    public virtual DjangoContentType ContentType { get; set; } = null!;
}
