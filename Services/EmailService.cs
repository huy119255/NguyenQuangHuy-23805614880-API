
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NguyenQuangHuy_23805614880_API.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOtpAsync(string receiver, string otp)
    {
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Chua cau hinh Gmail SMTP.");
        }

        password = password.Replace(" ", "").Trim();

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress("Cham Cong Nhom 6", username));

        message.To.Add(MailboxAddress.Parse(receiver));

        message.Subject =
            "Ma xac thuc OTP - Cham Cong Nhom 6";

        message.Body = new TextPart("plain")
        {
            Text = $"""
                Ma OTP cua ban la: {otp}

                Ma co hieu luc trong 5 phut.

                Khong chia se ma OTP voi nguoi khac.

                Cham Cong Nhom 6
                """
        };

        using var client = new SmtpClient
        {
            Timeout = 20000
        };

        string stage = "connect";

        try
        {
            // Ket noi Gmail bang SSL/TLS cong 465
            await client.ConnectAsync(
                "smtp.gmail.com",
                465,
                SecureSocketOptions.SslOnConnect);

            stage = "authenticate";

            // Thu xac thuc bang co che LOGIN
            await client.AuthenticateAsync(
                new SaslMechanismLogin(username, password));

            stage = "send";

            await client.SendAsync(message);

            _logger.LogInformation(
                "Gui OTP Gmail thanh cong.");

            stage = "disconnect";

            try
            {
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Email da duoc SMTP chap nhan,
                // khong danh dau that bai vi loi disconnect.
                _logger.LogWarning(
                    "SMTP DISCONNECT ERROR TYPE: {ErrorType}",
                    ex.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "SMTP FAILED AT: {Stage}; ERROR TYPE: {ErrorType}",
                stage,
                ex.GetType().Name);

            throw;
        }
    }
}
