using System;
using System.Collections.Generic;

namespace DBContext;

public partial class SocialaccountSocialapp
{
    public int Id { get; set; }

    public string Provider { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string Secret { get; set; } = null!;

    public string Key { get; set; } = null!;

    public virtual ICollection<SocialaccountSocialappSite> SocialaccountSocialappSites { get; } = new List<SocialaccountSocialappSite>();

    public virtual ICollection<SocialaccountSocialtoken> SocialaccountSocialtokens { get; } = new List<SocialaccountSocialtoken>();
}
