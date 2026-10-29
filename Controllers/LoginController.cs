
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NguyenQuangHuy_23805614880_API.Data;
using NguyenQuangHuy_23805614880_API.Models;
using NguyenQuangHuy_23805614880_API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NguyenQuangHuy_23805614880_API.Controllers;

public record LoginRequest(string Email, string Password);
public record LoginOtpRequest(string Email, string Code);

[ApiController]
[Route("api/auth")]
public class LoginController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;
    private readonly IConfiguration _config;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly PasswordHasher<EmailOtp> _otpHasher = new();

    public LoginController(
        AppDbContext db,
        EmailService email,
        IConfiguration config,
        IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _email = email;
        _config = config;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        string email = (request.Email ?? "")
            .Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null || !user.IsEmailVerified)
            return Unauthorized(new {
                message = "Tai khoan hoac mat khau khong hop le."
            });

        var check = _passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password ?? "");

        if (check == PasswordVerificationResult.Failed)
            return Unauthorized(new {
                message = "Tai khoan hoac mat khau khong hop le."
            });

        var now = DateTime.UtcNow;

        var latest = await _db.EmailOtps
            .Where(x => x.UserId == user.Id &&
                        x.Purpose == "Login")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (latest != null && latest.UsedAt == null &&
            latest.ExpiresAt > now.AddMinutes(4))
            return StatusCode(429, new {
                message = "Vui long cho 60 giay truoc khi gui lai OTP."
            });

        string code = RandomNumberGenerator
            .GetInt32(0, 1000000).ToString("D6");

        var otp = new EmailOtp
        {
            UserId = user.Id,
            Purpose = "Login",
            ExpiresAt = now.AddMinutes(5),
            AttemptCount = 0
        };

        otp.CodeHash = _otpHasher.HashPassword(otp, code);

        _db.EmailOtps.Add(otp);
        await _db.SaveChangesAsync();

        try
        {
            await _email.SendOtpAsync(email, code);
        }
        catch
        {
            _db.EmailOtps.Remove(otp);
            await _db.SaveChangesAsync();

            return StatusCode(502, new {
                message = "Khong gui duoc OTP dang nhap."
            });
        }

        return Ok(new {
            message = "Da xu ly yeu cau gui OTP dang nhap.",
            expiresInSeconds = 300
        });
    }

    [HttpPost("verify-login-otp")]
    public async Task<IActionResult> VerifyLoginOtp(
        LoginOtpRequest request)
    {
        string email = (request.Email ?? "")
            .Trim().ToLowerInvariant();

        string code = (request.Code ?? "").Trim();

        if (code.Length != 6 || !code.All(char.IsAsciiDigit))
            return BadRequest(new {
                message = "OTP phai gom 6 chu so."
            });

        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null || !user.IsEmailVerified)
            return Unauthorized(new {
                message = "OTP khong hop le."
            });

        var otp = await _db.EmailOtps
            .Where(x => x.UserId == user.Id &&
                        x.Purpose == "Login")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (otp == null || otp.UsedAt != null ||
            otp.ExpiresAt <= DateTime.UtcNow ||
            otp.AttemptCount >= 5)
            return Unauthorized(new {
                message = "OTP het han hoac khong hop le."
            });

        otp.AttemptCount++;

        var check = _otpHasher.VerifyHashedPassword(
            otp, otp.CodeHash, code);

        if (check == PasswordVerificationResult.Failed)
        {
            await _db.SaveChangesAsync();

            return Unauthorized(new {
                message = "OTP khong dung."
            });
        }

        otp.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Chua cau hinh Jwt:Key.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier,
                user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: "ChamCongNhom6",
            audience: "ChamCongNhom6Mobile",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(
                    Convert.FromBase64String(key)),
                SecurityAlgorithms.HmacSha256)
        );

        return Ok(new {
            message = "Dang nhap thanh cong.",
            accessToken = new JwtSecurityTokenHandler()
                .WriteToken(token),
            role = user.Role,
            fullName = user.FullName
        });
    }
}
