using System;
using System.Collections.Generic;

namespace EF;

public partial class AuthUserGroup
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int GroupId { get; set; }

    public virtual AuthGroup Group { get; set; } = null!;

    public virtual AuthUser User { get; set; } = null!;
}
