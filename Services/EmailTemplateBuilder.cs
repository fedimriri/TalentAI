namespace TalentAI.Services;

/// <summary>
/// Builds branded HTML email templates with glassmorphism dark-theme styling.
/// Each template uses status-specific color themes for visual distinction.
/// </summary>
public static class EmailTemplateBuilder
{
    private const string CompanyName = "TalentAI";

    /// <summary>
    /// HR Account Creation email — Purple theme (#8b5cf6)
    /// </summary>
    public static string BuildHRCredentialsTemplate(string email, string temporaryPassword)
    {
        return WrapInLayout(
            headerColor1: "#8b5cf6",
            headerColor2: "#7c3aed",
            accentColor: "#8b5cf6",
            headerTitle: "Welcome Aboard",
            headerSubtitle: "Your HR Account is Ready",
            body: $@"
                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 16px;'>
                    Hello,
                </p>
                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 20px;'>
                    Your HR account on <strong style='color: #e2e8f0;'>{CompanyName}</strong> has been successfully created. Below are your login credentials:
                </p>

                <!-- Credential Card -->
                <div style='background: rgba(139, 92, 246, 0.1); border: 1px solid rgba(139, 92, 246, 0.3); border-radius: 12px; padding: 24px; margin: 24px 0;'>
                    <table style='width: 100%;' cellpadding='0' cellspacing='0'>
                        <tr>
                            <td style='padding: 8px 0;'>
                                <span style='color: #94a3b8; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Login Email</span><br/>
                                <span style='color: #e2e8f0; font-size: 16px; font-weight: 600;'>{email}</span>
                            </td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; border-top: 1px solid rgba(139, 92, 246, 0.2);'>
                                <span style='color: #94a3b8; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Temporary Password</span><br/>
                                <span style='color: #e2e8f0; font-size: 16px; font-weight: 600; font-family: monospace; background: rgba(139, 92, 246, 0.15); padding: 4px 10px; border-radius: 6px; display: inline-block; margin-top: 4px;'>{temporaryPassword}</span>
                            </td>
                        </tr>
                    </table>
                </div>

                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 24px;'>
                    Please log in and change your password after your first login.
                </p>

                <!-- CTA Button -->
                <div style='text-align: center; margin: 28px 0;'>
                    <a href='http://localhost:5000/' style='display: inline-block; padding: 14px 40px; color: #ffffff; background: linear-gradient(135deg, #8b5cf6, #7c3aed); text-decoration: none; border-radius: 10px; font-size: 16px; font-weight: 700; letter-spacing: 0.5px; box-shadow: 0 4px 15px rgba(139, 92, 246, 0.4);'>
                        Log in to {CompanyName}
                    </a>
                </div>

                <p style='font-size: 13px; color: #64748b; line-height: 1.6; margin: 20px 0 0; text-align: center;'>
                    If you did not expect this email, please contact your administrator.
                </p>"
        );
    }

    /// <summary>
    /// Application Approved email — Green theme (#22c55e)
    /// </summary>
    public static string BuildApprovedTemplate(string candidateEmail, string jobTitle)
    {
        return WrapInLayout(
            headerColor1: "#22c55e",
            headerColor2: "#16a34a",
            accentColor: "#22c55e",
            headerTitle: "Congratulations!",
            headerSubtitle: "Your Application Has Been Accepted",
            body: $@"
                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 20px;'>
                    Dear Candidate,
                </p>

                <!-- Success Badge -->
                <div style='text-align: center; margin: 24px 0;'>
                    <div style='display: inline-block; background: rgba(34, 197, 94, 0.1); border: 1px solid rgba(34, 197, 94, 0.3); border-radius: 50px; padding: 10px 28px;'>
                        <span style='color: #22c55e; font-size: 14px; font-weight: 700; text-transform: uppercase; letter-spacing: 2px;'>✓ APPROVED</span>
                    </div>
                </div>

                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 16px;'>
                    We are pleased to inform you that your application for the position of
                    <strong style='color: #22c55e;'>&quot;{jobTitle}&quot;</strong>
                    at <strong style='color: #e2e8f0;'>{CompanyName}</strong> has been <strong style='color: #22c55e;'>approved</strong>.
                </p>
                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 16px;'>
                    Our team was impressed with your profile and experience. We will be in touch with you shortly regarding the next steps in the hiring process.
                </p>

                <!-- Next Steps Card -->
                <div style='background: rgba(34, 197, 94, 0.08); border: 1px solid rgba(34, 197, 94, 0.25); border-radius: 12px; padding: 20px; margin: 24px 0;'>
                    <p style='color: #22c55e; font-size: 14px; font-weight: 700; margin: 0 0 10px; text-transform: uppercase; letter-spacing: 1px;'>What's Next?</p>
                    <p style='color: #94a3b8; font-size: 14px; line-height: 1.6; margin: 0;'>
                        A member of our hiring team will reach out to you within the coming days to discuss onboarding details and next steps. Please keep an eye on your inbox.
                    </p>
                </div>

                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0;'>
                    Thank you for your interest in joining our team!
                </p>"
        );
    }

    /// <summary>
    /// Application Rejected email — Red theme (#ef4444)
    /// </summary>
    public static string BuildRejectedTemplate(string candidateEmail, string jobTitle)
    {
        return WrapInLayout(
            headerColor1: "#ef4444",
            headerColor2: "#dc2626",
            accentColor: "#ef4444",
            headerTitle: "Application Update",
            headerSubtitle: "Regarding Your Application",
            body: $@"
                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 20px;'>
                    Dear Candidate,
                </p>

                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 16px;'>
                    Thank you for your interest in the position of
                    <strong style='color: #e2e8f0;'>&quot;{jobTitle}&quot;</strong>
                    at <strong style='color: #e2e8f0;'>{CompanyName}</strong>.
                    We truly appreciate the time and effort you invested in your application.
                </p>
                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 16px;'>
                    After careful consideration, we regret to inform you that we will not be moving forward with your application at this time.
                </p>

                <!-- Encouragement Card -->
                <div style='background: rgba(239, 68, 68, 0.06); border: 1px solid rgba(239, 68, 68, 0.2); border-radius: 12px; padding: 20px; margin: 24px 0;'>
                    <p style='color: #f87171; font-size: 14px; font-weight: 700; margin: 0 0 10px; text-transform: uppercase; letter-spacing: 1px;'>Don't Give Up</p>
                    <p style='color: #94a3b8; font-size: 14px; line-height: 1.6; margin: 0;'>
                        This decision does not diminish your qualifications. We encourage you to apply for future positions that match your skills and experience. New opportunities are posted regularly.
                    </p>
                </div>

                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0;'>
                    We wish you all the best in your career journey.
                </p>"
        );
    }

    /// <summary>
    /// Application Under Review email — Blue theme (#3b82f6)
    /// </summary>
    public static string BuildUnderReviewTemplate(string candidateEmail, string jobTitle, string status)
    {
        return WrapInLayout(
            headerColor1: "#3b82f6",
            headerColor2: "#2563eb",
            accentColor: "#3b82f6",
            headerTitle: "Application Received",
            headerSubtitle: "We're Reviewing Your Profile",
            body: $@"
                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 20px;'>
                    Dear Candidate,
                </p>

                <!-- Status Badge -->
                <div style='text-align: center; margin: 24px 0;'>
                    <div style='display: inline-block; background: rgba(59, 130, 246, 0.1); border: 1px solid rgba(59, 130, 246, 0.3); border-radius: 50px; padding: 10px 28px;'>
                        <span style='color: #3b82f6; font-size: 14px; font-weight: 700; text-transform: uppercase; letter-spacing: 2px;'>⏳ {status.ToUpper()}</span>
                    </div>
                </div>

                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 16px;'>
                    Your application for the position of
                    <strong style='color: #3b82f6;'>&quot;{jobTitle}&quot;</strong>
                    at <strong style='color: #e2e8f0;'>{CompanyName}</strong> has been received and is currently being reviewed by our team.
                </p>
                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0 0 16px;'>
                    Our hiring team is carefully evaluating your profile and will get back to you as soon as possible.
                </p>

                <!-- Timeline Card -->
                <div style='background: rgba(59, 130, 246, 0.08); border: 1px solid rgba(59, 130, 246, 0.25); border-radius: 12px; padding: 20px; margin: 24px 0;'>
                    <p style='color: #60a5fa; font-size: 14px; font-weight: 700; margin: 0 0 10px; text-transform: uppercase; letter-spacing: 1px;'>What to Expect</p>
                    <p style='color: #94a3b8; font-size: 14px; line-height: 1.6; margin: 0;'>
                        You will receive an email notification once your application status is updated. No further action is required from your side at this moment.
                    </p>
                </div>

                <p style='font-size: 16px; line-height: 1.7; color: #cbd5e1; margin: 0;'>
                    We appreciate your patience and interest in this opportunity.
                </p>"
        );
    }

    /// <summary>
    /// Wraps email body content in the shared dark-theme glassmorphism layout.
    /// </summary>
    private static string WrapInLayout(string headerColor1, string headerColor2, string accentColor,
        string headerTitle, string headerSubtitle, string body)
    {
        return $@"
<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'/></head>
<body style='margin: 0; padding: 0; background-color: #0f172a; font-family: Arial, Helvetica, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 40px 20px;'>

        <!-- Header -->
        <div style='background: linear-gradient(135deg, {headerColor1}, {headerColor2}); border-radius: 16px 16px 0 0; padding: 40px 30px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0 0 6px; font-size: 26px; font-weight: 800; letter-spacing: -0.5px;'>{headerTitle}</h1>
            <p style='color: rgba(255,255,255,0.85); margin: 0; font-size: 14px; letter-spacing: 0.5px;'>{headerSubtitle}</p>
        </div>

        <!-- Body Card (Glassmorphism) -->
        <div style='background: rgba(30, 41, 59, 0.95); border: 1px solid rgba(255,255,255,0.08); border-top: none; padding: 36px 30px;'>
            {body}
        </div>

        <!-- Footer -->
        <div style='background: rgba(15, 23, 42, 0.9); border: 1px solid rgba(255,255,255,0.05); border-top: none; border-radius: 0 0 16px 16px; padding: 24px 30px; text-align: center;'>
            <p style='color: {accentColor}; font-size: 16px; font-weight: 700; margin: 0 0 6px; letter-spacing: 0.5px;'>{CompanyName}</p>
            <p style='color: #475569; font-size: 12px; margin: 0; line-height: 1.5;'>
                This is an automated message. Please do not reply to this email.
            </p>
        </div>

    </div>
</body>
</html>";
    }
}
