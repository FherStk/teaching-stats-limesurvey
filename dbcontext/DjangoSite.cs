using System;
using System.Collections.Generic;

namespace DBContext;

public partial class DjangoSite
{
    public int Id { get; set; }

    public string Domain { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<SocialaccountSocialappSite> SocialaccountSocialappSites { get; } = new List<SocialaccountSocialappSite>();
}
