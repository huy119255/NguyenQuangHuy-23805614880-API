using System;
using System.Collections.Generic;

namespace NguyenQuangHuy_23805614880_API.Models;

public partial class EmailOtp
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string CodeHash { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public int AttemptCount { get; set; }

    public virtual User User { get; set; } = null!;
}
