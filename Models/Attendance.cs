using System;
using System.Collections.Generic;

namespace NguyenQuangHuy_23805614880_API.Models;

public partial class Attendance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string CheckInMethod { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
