using System;
using System.Collections.Generic;

namespace DBContext;

public partial class AuthUser
{
    public int Id { get; set; }

    public string Password { get; set; } = null!;

    public DateTime? LastLogin { get; set; }

    public bool IsSuperuser { get; set; }

    public string Username { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool IsStaff { get; set; }

    public bool IsActive { get; set; }

    public DateTime DateJoined { get; set; }

    public virtual ICollection<AccountEmailaddress> AccountEmailaddresses { get; } = new List<AccountEmailaddress>();

    public virtual ICollection<AuthUserGroup> AuthUserGroups { get; } = new List<AuthUserGroup>();

    public virtual ICollection<AuthUserUserPermission> AuthUserUserPermissions { get; } = new List<AuthUserUserPermission>();

    public virtual ICollection<DjangoAdminLog> DjangoAdminLogs { get; } = new List<DjangoAdminLog>();

    public virtual ICollection<SocialaccountSocialaccount> SocialaccountSocialaccounts { get; } = new List<SocialaccountSocialaccount>();
}
