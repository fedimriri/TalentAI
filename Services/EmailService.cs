// ============================================================
// EmailService — SendGrid API (PRODUCTION / Railway)
// ============================================================
// Gmail SMTP (MailKit) is commented out below. To switch back
// to SMTP for local development, uncomment the SMTP section
// and comment out the SendGrid section.
// ============================================================

using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Options;
using TalentAI.Configurations;

// --- SMTP imports (uncomment if switching back to MailKit) ---
// using MailKit.Net.Smtp;
// using MailKit.Security;
// using MimeKit;
// using MimeKit.Text;

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
            var subject = "Your TalentAI HR Account Credentials";
            var htmlBody = EmailTemplateBuilder.BuildHRCredentialsTemplate(toEmail, temporaryPassword);

            await SendEmailAsync(toEmail, subject, htmlBody);
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

            await SendEmailAsync(toEmail, subject, htmlBody);
            _logger.LogInformation("Email successfully sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }

    // ============================================================
    // ACTIVE: SendGrid API (HTTPS only — no SMTP ports needed)
    // ============================================================
    /// <summary>
    /// Sends an email via SendGrid API with retry logic.
    /// Uses HTTPS API calls — no outbound SMTP ports required (Railway-compatible).
    /// </summary>
    private async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        const int maxRetries = 3;

        var client = new SendGridClient(_settings.SendGridApiKey);
        var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    return; // Success
                }

                var body = await response.Body.ReadAsStringAsync();
                _logger.LogWarning("SendGrid returned {StatusCode} on attempt {Attempt}: {Body}",
                    response.StatusCode, attempt, body);

                if (attempt >= maxRetries)
                {
                    throw new Exception($"SendGrid failed after {maxRetries} attempts. " +
                        $"Last status: {response.StatusCode}, Body: {body}");
                }
            }
            catch (Exception) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)); // 1s, 2s, 4s
                _logger.LogWarning("SendGrid attempt {Attempt}/{MaxRetries} failed, retrying in {Delay}s...",
                    attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }
    }

    // ============================================================
    // COMMENTED OUT: Gmail SMTP via MailKit (for local development)
    // ============================================================
    // To use SMTP locally:
    //   1. Uncomment the SMTP imports at the top of this file
    //   2. Uncomment the method below
    //   3. Comment out the SendGrid SendEmailAsync method above
    //   4. Update EmailSettings.cs to use SMTP properties
    //   5. Set EMAIL_HOST, EMAIL_PORT, EMAIL_USERNAME, EMAIL_PASSWORD in .env
    // ============================================================
    //
    // private async Task SendEmailAsync(MimeMessage email)
    // {
    //     const int maxRetries = 3;
    //
    //     for (int attempt = 1; attempt <= maxRetries; attempt++)
    //     {
    //         try
    //         {
    //             using var smtp = new SmtpClient();
    //             await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
    //             await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
    //             await smtp.SendAsync(email);
    //             await smtp.DisconnectAsync(true);
    //             return;
    //         }
    //         catch (Exception) when (attempt < maxRetries)
    //         {
    //             var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
    //             _logger.LogWarning("SMTP attempt {Attempt}/{MaxRetries} failed, retrying in {Delay}s...",
    //                 attempt, maxRetries, delay.TotalSeconds);
    //             await Task.Delay(delay);
    //         }
    //     }
    // }
}
