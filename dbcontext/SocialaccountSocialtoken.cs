using System;
using System.Collections.Generic;

namespace DBContext;

public partial class SocialaccountSocialtoken
{
    public int Id { get; set; }

    public string Token { get; set; } = null!;

    public string TokenSecret { get; set; } = null!;

    public DateTime? ExpiresAt { get; set; }

    public int AccountId { get; set; }

    public int AppId { get; set; }

    public virtual SocialaccountSocialaccount Account { get; set; } = null!;

    public virtual SocialaccountSocialapp App { get; set; } = null!;
}
