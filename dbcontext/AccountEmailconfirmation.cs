using System;
using System.Collections.Generic;

namespace DBContext;

public partial class AccountEmailconfirmation
{
    public int Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Sent { get; set; }

    public string Key { get; set; } = null!;

    public int EmailAddressId { get; set; }

    public virtual AccountEmailaddress EmailAddress { get; set; } = null!;
}
