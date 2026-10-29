using System;
using System.Collections.Generic;

namespace NguyenQuangHuy_23805614880_API.Models;

public partial class Employee
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string? Department { get; set; }

    public string? Position { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual User User { get; set; } = null!;
}
