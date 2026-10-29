
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenQuangHuy_23805614880_API.Data;
using NguyenQuangHuy_23805614880_API.Models;
using NguyenQuangHuy_23805614880_API.Services;
using System.Security.Cryptography;

namespace NguyenQuangHuy_23805614880_API.Controllers;

public record SendOtpRequest(string Email);
public record VerifyOtpRequest(string Email, string Code);

[ApiController]
[Route("api/auth")]
public class OtpController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;
    private readonly ILogger<OtpController> _logger;
    private readonly PasswordHasher<EmailOtp> _hasher = new();

    public OtpController(
        AppDbContext db,
        EmailService emailService,
        ILogger<OtpController> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    // ==========================================
    // 1. GUI OTP XAC THUC EMAIL
    // POST: /api/auth/send-otp
    // ==========================================

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendOtpRequest request)
    {
        string email = (request.Email ?? "")
            .Trim()
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Vui long nhap email."
            });
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        // Khong tiet lo tai khoan co ton tai hay khong
        if (user == null || user.IsEmailVerified)
        {
            return Ok(new
            {
                message = "Neu tai khoan can xac thuc, OTP se duoc gui."
            });
        }

        DateTime now = DateTime.UtcNow;

        // Tim OTP moi nhat
        var latest = await _db.EmailOtps
            .Where(o =>
                o.UserId == user.Id &&
                o.Purpose == "VerifyEmail")
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();

        // Gioi han gui lai OTP trong 60 giay
        if (latest != null &&
            latest.UsedAt == null &&
            latest.ExpiresAt > now.AddMinutes(4))
        {
            return StatusCode(429, new
            {
                message = "Vui long cho 60 giay truoc khi gui lai OTP."
            });
        }

        // Tao OTP ngau nhien 6 chu so
        string code = RandomNumberGenerator
            .GetInt32(0, 1000000)
            .ToString("D6");

        var otp = new EmailOtp
        {
            UserId = user.Id,
            Purpose = "VerifyEmail",
            ExpiresAt = now.AddMinutes(5),
            AttemptCount = 0
        };

        // Bam OTP truoc khi luu vao SQL Server
        otp.CodeHash = _hasher.HashPassword(otp, code);

        _db.EmailOtps.Add(otp);
        await _db.SaveChangesAsync();

        try
        {
            // Gui OTP qua Gmail SMTP
            await _emailService.SendOtpAsync(email, code);

            _logger.LogInformation(
                "OTP email verification sent for user ID {UserId}",
                user.Id);
        }
        catch (Exception ex)
        {
            // Chi ghi loai loi, khong ghi mat khau hay OTP
            _logger.LogError(
                "SMTP ERROR TYPE: {ErrorType}",
                ex.GetType().Name);

            // Huy OTP neu gui email that bai
            _db.EmailOtps.Remove(otp);
            await _db.SaveChangesAsync();

            return StatusCode(502, new
            {
                message = "Khong gui duoc email OTP. Kiem tra Gmail SMTP.",
                errorType = ex.GetType().Name
            });
        }

        return Ok(new
        {
            message = "Yeu cau gui OTP da duoc xu ly.",
            expiresInSeconds = 300
        });
    }

    // ==========================================
    // 2. XAC THUC OTP EMAIL
    // POST: /api/auth/verify-otp
    // ==========================================

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request)
    {
        string email = (request.Email ?? "")
            .Trim()
            .ToLowerInvariant();

        string code = (request.Code ?? "").Trim();

        if (code.Length != 6 ||
            !code.All(char.IsAsciiDigit))
        {
            return BadRequest(new
            {
                message = "Ma OTP phai gom dung 6 chu so."
            });
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return BadRequest(new
            {
                message = "Ma OTP khong hop le."
            });
        }

        if (user.IsEmailVerified)
        {
            return Ok(new
            {
                message = "Email da duoc xac thuc.",
                verified = true
            });
        }

        // Lay OTP moi nhat chua su dung
        var otp = await _db.EmailOtps
            .Where(o =>
                o.UserId == user.Id &&
                o.Purpose == "VerifyEmail" &&
                o.UsedAt == null)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            return BadRequest(new
            {
                message = "Khong tim thay OTP hop le."
            });
        }

        if (otp.ExpiresAt <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Ma OTP da het han."
            });
        }

        if (otp.AttemptCount >= 5)
        {
            return BadRequest(new
            {
                message = "Da nhap sai OTP qua 5 lan."
            });
        }

        otp.AttemptCount++;

        var result = _hasher.VerifyHashedPassword(
            otp,
            otp.CodeHash,
            code);

        if (result == PasswordVerificationResult.Failed)
        {
            await _db.SaveChangesAsync();

            return BadRequest(new
            {
                message = "Ma OTP khong dung."
            });
        }

        // Xac thuc email thanh cong
        user.IsEmailVerified = true;
        otp.UsedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Xac thuc email thanh cong.",
            verified = true,
            userId = user.Id
        });
    }
}
