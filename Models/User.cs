using System;
using System.Collections.Generic;

namespace NguyenQuangHuy_23805614880_API.Models;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsEmailVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<EmailOtp> EmailOtps { get; set; } = new List<EmailOtp>();

    public virtual Employee? Employee { get; set; }
}
