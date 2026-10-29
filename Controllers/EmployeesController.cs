using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenQuangHuy_23805614880_API.Data;
using NguyenQuangHuy_23805614880_API.Models;
using System.ComponentModel.DataAnnotations;

namespace NguyenQuangHuy_23805614880_API.Controllers;

public sealed record CreateEmployeeRequest
{
    [Required, MaxLength(100)] public required string FullName { get; init; }
    [Required, EmailAddress, MaxLength(255)] public required string Email { get; init; }
    [Required, MinLength(8)] public required string Password { get; init; }
    [Required, MaxLength(30)] public required string EmployeeCode { get; init; }
    [MaxLength(100)] public string? Department { get; init; }
    [MaxLength(100)] public string? Position { get; init; }
}

public sealed record UpdateEmployeeRequest
{
    [Required, MaxLength(100)] public required string FullName { get; init; }
    [Required, MaxLength(30)] public required string EmployeeCode { get; init; }
    [MaxLength(100)] public string? Department { get; init; }
    [MaxLength(100)] public string? Position { get; init; }
    public bool IsActive { get; init; }
}

public sealed record EmployeeResponse(
    int Id, string EmployeeCode, string FullName, string Email,
    string? Department, string? Position, bool IsActive);

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "Admin")]
public sealed class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public EmployeesController(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var employees = await _db.Employees.Include(x => x.User)
            .OrderBy(x => x.EmployeeCode)
            .Select(x => new EmployeeResponse(x.Id, x.EmployeeCode, x.User.FullName,
                x.User.Email, x.Department, x.Position, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(employees);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create(
        CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return Conflict(new { message = "Email da ton tai." });
        if (await _db.Employees.AnyAsync(x => x.EmployeeCode == request.EmployeeCode, cancellationToken))
            return Conflict(new { message = "Ma nhan vien da ton tai." });

        var user = new User
        {
            FullName = request.FullName.Trim(), Email = email, Role = "Employee",
            IsEmailVerified = true, CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        var employee = new Employee
        {
            User = user, EmployeeCode = request.EmployeeCode.Trim(),
            Department = request.Department?.Trim(), Position = request.Position?.Trim(), IsActive = true
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);
        var response = new EmployeeResponse(employee.Id, employee.EmployeeCode, user.FullName,
            user.Email, employee.Department, employee.Position, employee.IsActive);
        return CreatedAtAction(nameof(GetAll), new { id = employee.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeResponse>> Update(
        int id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (employee is null) return NotFound();
        var employeeCode = request.EmployeeCode.Trim();
        var duplicateCode = await _db.Employees.AnyAsync(
            x => x.Id != id && x.EmployeeCode == employeeCode,
            cancellationToken);
        if (duplicateCode)
            return Conflict(new { message = "Ma nhan vien da ton tai." });

        employee.User.FullName = request.FullName.Trim();
        employee.EmployeeCode = employeeCode;
        employee.Department = request.Department?.Trim();
        employee.Position = request.Position?.Trim();
        employee.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new EmployeeResponse(employee.Id, employee.EmployeeCode, employee.User.FullName,
            employee.User.Email, employee.Department, employee.Position, employee.IsActive));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (employee is null) return NotFound();
        employee.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}