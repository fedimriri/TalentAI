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

            var htmlBody = EmailTemplateBuilder.BuildHRCredentialsTemplate(toEmail, temporaryPassword);
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
            string htmlBody;

            switch (status)
            {
                case "Approved":
                    subject = "Application Accepted — TalentAI";
                    htmlBody = EmailTemplateBuilder.BuildApprovedTemplate(toEmail, jobTitle);
                    break;

                case "Rejected":
                    subject = "Application Update — TalentAI";
                    htmlBody = EmailTemplateBuilder.BuildRejectedTemplate(toEmail, jobTitle);
                    break;

                default: // "Under Review", "Shortlisted", etc.
                    subject = "Application Received — TalentAI";
                    htmlBody = EmailTemplateBuilder.BuildUnderReviewTemplate(toEmail, jobTitle, status);
                    break;
            }

            email.Subject = subject;
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
    /// Shared SMTP send logic with retry: connect via STARTTLS, authenticate, send, disconnect.
    /// Retries up to 3 times with exponential backoff for transient network failures.
    /// </summary>
    private async Task SendEmailAsync(MimeMessage email)
    {
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                return; // Success — exit retry loop
            }
            catch (Exception) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)); // 1s, 2s, 4s
                _logger.LogWarning("SMTP attempt {Attempt}/{MaxRetries} failed, retrying in {Delay}s...", attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }
    }
}
