using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using TalentAI.Configurations;

namespace TalentAI.Services;

public class EmailService : IEmailService
{
    private readonly MailtrapSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<MailtrapSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendHRCredentialsAsync(string toEmail, string temporaryPassword)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("TalentAI Admin", _settings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Your TalentAI HR Account Credentials";

            var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h2>Welcome to TalentAI!</h2>
                <p>An administrator has created an HR account for you.</p>
                <p>Here are your access credentials:</p>
                <div style='background: #f4f4f4; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                    <strong>Username/Email:</strong> {toEmail}<br>
                    <strong>Temporary Password:</strong> {temporaryPassword}
                </div>
                <p>Please log in and update your profile as soon as possible.</p>
                <a href='http://localhost:5000/login' style='display: inline-block; padding: 10px 20px; color: white; background: #007bff; text-decoration: none; border-radius: 5px;'>Log in to TalentAI</a>
                <p style='margin-top: 20px; font-size: 0.9em; color: #777;'>If you did not expect this email, please contact the administrator.</p>
            </div>";

            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var smtp = new SmtpClient();
            
            // Connect to Mailtrap Sandbox SMTP
            await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Successfully sent HR credentials email to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HR credentials email to {Email}. Network or authentication issue.", toEmail);
        }
    }
}
