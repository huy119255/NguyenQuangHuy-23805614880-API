using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenQuangHuy_23805614880_API.Data;
using NguyenQuangHuy_23805614880_API.Models;
using System.ComponentModel.DataAnnotations;

namespace NguyenQuangHuy_23805614880_API.Controllers;

public sealed record CheckInRequest(
    decimal? Latitude,
    decimal? Longitude,
    [Required] string Method);

public sealed record AttendanceResponse(
    int Id,
    int EmployeeId,
    DateTime CheckInTime,
    DateTime? CheckOutTime,
    decimal? Latitude,
    decimal? Longitude,
    string CheckInMethod);

[ApiController]
[Route("api/attendance")]
[Authorize]
public sealed class AttendanceController : ControllerBase
{
    private readonly AppDbContext _db;

    public AttendanceController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<AttendanceResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var rows = await _db.Attendances
            .OrderByDescending(x => x.CheckInTime)
            .Select(x => new AttendanceResponse(
                x.Id,
                x.EmployeeId,
                x.CheckInTime,
                x.CheckOutTime,
                x.Latitude,
                x.Longitude,
                x.CheckInMethod))
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost("check-in")]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<AttendanceResponse>> CheckIn(
        CheckInRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out var userId))
            return Unauthorized();

        var employee = await _db.Employees
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.IsActive,
                cancellationToken);
        if (employee is null)
            return NotFound(new { message = "Khong tim thay ho so nhan vien." });

        var open = await _db.Attendances.AnyAsync(
            x => x.EmployeeId == employee.Id && x.CheckOutTime == null,
            cancellationToken);
        if (open)
            return Conflict(new {
                message = "Ban dang co ca lam viec chua check-out."
            });

        var attendance = new Attendance
        {
            EmployeeId = employee.Id,
            CheckInTime = DateTime.UtcNow,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CheckInMethod = request.Method.Trim()
        };
        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AttendanceResponse(
            attendance.Id,
            attendance.EmployeeId,
            attendance.CheckInTime,
            attendance.CheckOutTime,
            attendance.Latitude,
            attendance.Longitude,
            attendance.CheckInMethod));
    }

    [HttpPatch("{id:int}/check-out")]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<AttendanceResponse>> CheckOut(
        int id,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out var userId))
            return Unauthorized();

        var attendance = await _db.Attendances
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(
                x => x.Id == id && x.Employee.UserId == userId,
                cancellationToken);
        if (attendance is null)
            return NotFound();
        if (attendance.CheckOutTime is not null)
            return Conflict(new { message = "Ca lam viec da check-out." });

        attendance.CheckOutTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AttendanceResponse(
            attendance.Id,
            attendance.EmployeeId,
            attendance.CheckInTime,
            attendance.CheckOutTime,
            attendance.Latitude,
            attendance.Longitude,
            attendance.CheckInMethod));
    }
}