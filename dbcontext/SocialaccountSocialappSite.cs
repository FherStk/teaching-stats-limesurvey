using System;
using System.Collections.Generic;

namespace EF;

public partial class SocialaccountSocialappSite
{
    public int Id { get; set; }

    public int SocialappId { get; set; }

    public int SiteId { get; set; }

    public virtual DjangoSite Site { get; set; } = null!;

    public virtual SocialaccountSocialapp Socialapp { get; set; } = null!;
}
