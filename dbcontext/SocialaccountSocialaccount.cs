using System;
using System.Collections.Generic;

namespace DBContext;

public partial class SocialaccountSocialaccount
{
    public int Id { get; set; }

    public string Provider { get; set; } = null!;

    public string Uid { get; set; } = null!;

    public DateTime LastLogin { get; set; }

    public DateTime DateJoined { get; set; }

    public string ExtraData { get; set; } = null!;

    public int UserId { get; set; }

    public virtual ICollection<SocialaccountSocialtoken> SocialaccountSocialtokens { get; } = new List<SocialaccountSocialtoken>();

    public virtual AuthUser User { get; set; } = null!;
}
