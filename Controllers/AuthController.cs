
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenQuangHuy_23805614880_API.Data;
using NguyenQuangHuy_23805614880_API.Models;

namespace NguyenQuangHuy_23805614880_API.Controllers;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password
);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AuthController(
        AppDbContext db,
        IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        string email = (request.Email ?? "").Trim().ToLowerInvariant();
        string name = (request.FullName ?? "").Trim();

        if (name.Length < 2 ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                .IsValid(email) ||
            string.IsNullOrEmpty(request.Password) ||
            request.Password.Length < 8)
        {
            return BadRequest(new
            {
                message = "Thông tin không hợp lệ. Mật khẩu tối thiểu 8 ký tự."
            });
        }

        if (await _db.Users.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "Email đã được đăng ký." });

        var user = new User
        {
            FullName = name,
            Email = email,
            Role = "Employee",
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(
            user, request.Password);

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Không thể đăng ký. Hãy kiểm tra lại email."
            });
        }

        return StatusCode(201, new
        {
            message = "Đã tạo tài khoản, cần xác thực email.",
            userId = user.Id
        });
    }
}
