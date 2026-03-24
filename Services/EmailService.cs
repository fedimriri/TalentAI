using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using TalentAI.Configurations;

namespace TalentAI.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendHRCredentialsAsync(string toEmail, string temporaryPassword)
    {
        if (string.IsNullOrEmpty(toEmail)) return;

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("TalentAI", _settings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Your TalentAI HR Account Credentials";

            var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333;'>
                <div style='background: linear-gradient(135deg, #007bff, #0056b3); padding: 30px; border-radius: 8px 8px 0 0; text-align: center;'>
                    <h1 style='color: #ffffff; margin: 0; font-size: 24px;'>TalentAI</h1>
                </div>
                <div style='padding: 30px; background: #ffffff; border: 1px solid #e0e0e0;'>
                    <p style='font-size: 16px; line-height: 1.6;'>Hello,</p>
                    <p style='font-size: 16px; line-height: 1.6;'>Your HR account has been successfully created.</p>
                    <div style='background: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #007bff;'>
                        <p style='margin: 5px 0;'><strong>Login Email:</strong> {toEmail}</p>
                        <p style='margin: 5px 0;'><strong>Temporary Password:</strong> {temporaryPassword}</p>
                    </div>
                    <p style='font-size: 16px; line-height: 1.6;'>Login here:</p>
                    <div style='text-align: center; margin: 25px 0;'>
                        <a href='http://localhost:5000/' style='display: inline-block; padding: 12px 30px; color: #ffffff; background: #007bff; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: bold;'>Log in to TalentAI</a>
                    </div>
                    <p style='font-size: 14px; color: #777; line-height: 1.6;'>Please change your password after your first login.</p>
                </div>
                <div style='padding: 15px; text-align: center; font-size: 12px; color: #999; background: #f8f9fa; border-radius: 0 0 8px 8px;'>
                    <p style='margin: 0;'>Best regards, TalentAI Team</p>
                </div>
            </div>";

            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            await SendEmailAsync(email);
            _logger.LogInformation("Email successfully sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }

    public async Task SendCandidateStatusEmailAsync(string toEmail, string status, string jobTitle)
    {
        if (string.IsNullOrEmpty(toEmail)) return;

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("TalentAI", _settings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));

            string subject;
            string bodyContent;

            switch (status)
            {
                case "Approved":
                    subject = "Application Accepted — TalentAI";
                    bodyContent = $@"
                        <h2 style='color: #28a745;'>Congratulations!</h2>
                        <p style='font-size: 16px; line-height: 1.6;'>Your application for &quot;{jobTitle}&quot; has been approved.</p>
                        <p style='font-size: 16px; line-height: 1.6;'>Our team will contact you soon.</p>";
                    break;

                case "Rejected":
                    subject = "Application Update — TalentAI";
                    bodyContent = $@"
                        <h2 style='color: #6c757d;'>Application Update</h2>
                        <p style='font-size: 16px; line-height: 1.6;'>Thank you for your interest in &quot;{jobTitle}&quot;.</p>
                        <p style='font-size: 16px; line-height: 1.6;'>Unfortunately, we will not proceed with your application at this time.</p>
                        <p style='font-size: 16px; line-height: 1.6;'>We encourage you to apply for future opportunities.</p>";
                    break;

                default: // "Under Review", "Shortlisted", etc.
                    subject = "Application Received — TalentAI";
                    bodyContent = $@"
                        <h2 style='color: #007bff;'>Application Received</h2>
                        <p style='font-size: 16px; line-height: 1.6;'>Your application for &quot;{jobTitle}&quot; is currently under review.</p>
                        <p style='font-size: 16px; line-height: 1.6;'>We will notify you once there is an update.</p>";
                    break;
            }

            email.Subject = subject;

            var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333;'>
                <div style='background: linear-gradient(135deg, #007bff, #0056b3); padding: 30px; border-radius: 8px 8px 0 0; text-align: center;'>
                    <h1 style='color: #ffffff; margin: 0; font-size: 24px;'>TalentAI</h1>
                </div>
                <div style='padding: 30px; background: #ffffff; border: 1px solid #e0e0e0;'>
                    {bodyContent}
                </div>
                <div style='padding: 15px; text-align: center; font-size: 12px; color: #999; background: #f8f9fa; border-radius: 0 0 8px 8px;'>
                    <p style='margin: 0;'>This is an automated message from TalentAI. Please do not reply to this email.</p>
                </div>
            </div>";

            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            await SendEmailAsync(email);
            _logger.LogInformation("Email successfully sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }

    /// <summary>
    /// Shared SMTP send logic: connect via STARTTLS, authenticate, send, disconnect.
    /// </summary>
    private async Task SendEmailAsync(MimeMessage email)
    {
        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
