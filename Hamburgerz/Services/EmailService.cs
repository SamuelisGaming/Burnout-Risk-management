using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Hamburgerz.Services
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(EmailSettings settings)
        {
            _settings = settings;
        }

        public async Task SendEmailAsync(string toAddress, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendVerificationEmailAsync(string toAddress, string username, string verificationUrl, bool isEnglish)
        {
            string subject = isEnglish ? "Confirm your email – Burnout Risk" : "Patvirtinkite el. paštą – Burnout Risk";
            string html = isEnglish
                ? BuildVerificationEmailEn(username, verificationUrl)
                : BuildVerificationEmailLt(username, verificationUrl);

            await SendEmailAsync(toAddress, subject, html);
        }

        public async Task SendPasswordResetEmailAsync(string toAddress, string username, string resetUrl, bool isEnglish)
        {
            string subject = isEnglish ? "Reset your password – Burnout Risk" : "Slaptažodžio atstatymas – Burnout Risk";
            string html = isEnglish
                ? BuildPasswordResetEmailEn(username, resetUrl)
                : BuildPasswordResetEmailLt(username, resetUrl);

            await SendEmailAsync(toAddress, subject, html);
        }

        private static string BuildVerificationEmailLt(string username, string url) => $"""
            <div style="font-family:sans-serif;max-width:520px;margin:0 auto;padding:32px 24px;background:#f8fafc;border-radius:16px">
              <h2 style="color:#0f172a;margin-bottom:8px">Sveiki, {username}!</h2>
              <p style="color:#475569;line-height:1.7">Norėdami aktyvuoti paskyrą, paspauskite mygtuką žemiau. Nuoroda galioja <strong>24 valandas</strong>.</p>
              <div style="text-align:center;margin:32px 0">
                <a href="{url}" style="background:#0f172a;color:#fff;padding:14px 32px;border-radius:999px;text-decoration:none;font-weight:700;font-size:15px">Patvirtinti el. paštą</a>
              </div>
              <p style="color:#94a3b8;font-size:13px">Jei nesate registravęsi šioje sistemoje, galite ignoruoti šį laišką.</p>
              <hr style="border:none;border-top:1px solid #e2e8f0;margin:24px 0">
              <p style="color:#94a3b8;font-size:12px">Burnout Risk Management &mdash; universiteto projektas</p>
            </div>
            """;

        private static string BuildVerificationEmailEn(string username, string url) => $"""
            <div style="font-family:sans-serif;max-width:520px;margin:0 auto;padding:32px 24px;background:#f8fafc;border-radius:16px">
              <h2 style="color:#0f172a;margin-bottom:8px">Hello, {username}!</h2>
              <p style="color:#475569;line-height:1.7">To activate your account, click the button below. The link is valid for <strong>24 hours</strong>.</p>
              <div style="text-align:center;margin:32px 0">
                <a href="{url}" style="background:#0f172a;color:#fff;padding:14px 32px;border-radius:999px;text-decoration:none;font-weight:700;font-size:15px">Verify email</a>
              </div>
              <p style="color:#94a3b8;font-size:13px">If you did not register on this platform, you can safely ignore this email.</p>
              <hr style="border:none;border-top:1px solid #e2e8f0;margin:24px 0">
              <p style="color:#94a3b8;font-size:12px">Burnout Risk Management &mdash; Hamburgerz</p>
            </div>
            """;

        private static string BuildPasswordResetEmailLt(string username, string url) => $"""
            <div style="font-family:sans-serif;max-width:520px;margin:0 auto;padding:32px 24px;background:#f8fafc;border-radius:16px">
              <h2 style="color:#0f172a;margin-bottom:8px">Sveiki, {username}!</h2>
              <p style="color:#475569;line-height:1.7">Gavome prašymą atstatyti slaptažodį. Paspauskite mygtuką žemiau. Nuoroda galioja <strong>1 valandą</strong>.</p>
              <div style="text-align:center;margin:32px 0">
                <a href="{url}" style="background:#0f172a;color:#fff;padding:14px 32px;border-radius:999px;text-decoration:none;font-weight:700;font-size:15px">Atstatyti slaptažodį</a>
              </div>
              <p style="color:#94a3b8;font-size:13px">Jei neprašėte atstatyti slaptažodžio, galite ignoruoti šį laišką.</p>
              <hr style="border:none;border-top:1px solid #e2e8f0;margin:24px 0">
              <p style="color:#94a3b8;font-size:12px">Burnout Risk Management &mdash; universiteto projektas</p>
            </div>
            """;

        public async Task SendBugReportEmailAsync(string topic, string description, string username, string userEmail, string deviceInfo)
        {
            string subject = $"[Bug Report] {topic} — {username}";
            string html = BuildBugReportEmail(topic, description, username, userEmail, deviceInfo);
            await SendEmailAsync(_settings.FromAddress, subject, html);
        }

        private static string BuildBugReportEmail(string topic, string description, string username, string userEmail, string deviceInfo) => $"""
            <div style="font-family:sans-serif;max-width:600px;margin:0 auto;padding:32px 24px;background:#f8fafc;border-radius:16px">
              <div style="background:#0f172a;color:#fff;padding:18px 24px;border-radius:12px;margin-bottom:24px">
                <div style="font-size:11px;letter-spacing:1px;text-transform:uppercase;opacity:0.6;margin-bottom:4px">Bug Report</div>
                <h2 style="margin:0;font-size:20px">{System.Net.WebUtility.HtmlEncode(topic)}</h2>
              </div>

              <table style="width:100%;border-collapse:collapse;margin-bottom:20px">
                <tr>
                  <td style="padding:10px 14px;background:#fff;border:1px solid #e2e8f0;border-radius:8px 8px 0 0;width:140px;font-size:12px;font-weight:700;color:#64748b;text-transform:uppercase;letter-spacing:0.5px">User</td>
                  <td style="padding:10px 14px;background:#fff;border:1px solid #e2e8f0;border-top:none;font-size:14px;color:#0f172a">{System.Net.WebUtility.HtmlEncode(username)} &lt;{System.Net.WebUtility.HtmlEncode(userEmail)}&gt;</td>
                </tr>
                <tr>
                  <td style="padding:10px 14px;background:#f8fafc;border:1px solid #e2e8f0;border-top:none;font-size:12px;font-weight:700;color:#64748b;text-transform:uppercase;letter-spacing:0.5px">Topic</td>
                  <td style="padding:10px 14px;background:#f8fafc;border:1px solid #e2e8f0;border-top:none;font-size:14px;color:#0f172a">{System.Net.WebUtility.HtmlEncode(topic)}</td>
                </tr>
                <tr>
                  <td style="padding:10px 14px;background:#fff;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 8px 8px;font-size:12px;font-weight:700;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;vertical-align:top">Device</td>
                  <td style="padding:10px 14px;background:#fff;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 8px 0;font-size:13px;color:#475569;font-family:monospace;white-space:pre-wrap">{System.Net.WebUtility.HtmlEncode(deviceInfo)}</td>
                </tr>
              </table>

              <div style="background:#fff;border:1px solid #e2e8f0;border-radius:8px;padding:16px 18px;margin-bottom:20px">
                <div style="font-size:12px;font-weight:700;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:8px">Description</div>
                <p style="margin:0;font-size:14px;color:#0f172a;line-height:1.7;white-space:pre-wrap">{System.Net.WebUtility.HtmlEncode(description)}</p>
              </div>

              <hr style="border:none;border-top:1px solid #e2e8f0;margin:20px 0">
              <p style="color:#94a3b8;font-size:12px;margin:0">Burnout Risk Management &mdash; automated bug report</p>
            </div>
            """;

        private static string BuildPasswordResetEmailEn(string username, string url) => $"""
            <div style="font-family:sans-serif;max-width:520px;margin:0 auto;padding:32px 24px;background:#f8fafc;border-radius:16px">
              <h2 style="color:#0f172a;margin-bottom:8px">Hello, {username}!</h2>
              <p style="color:#475569;line-height:1.7">We received a request to reset your password. Click the button below. The link is valid for <strong>1 hour</strong>.</p>
              <div style="text-align:center;margin:32px 0">
                <a href="{url}" style="background:#0f172a;color:#fff;padding:14px 32px;border-radius:999px;text-decoration:none;font-weight:700;font-size:15px">Reset password</a>
              </div>
              <p style="color:#94a3b8;font-size:13px">If you did not request a password reset, you can safely ignore this email.</p>
              <hr style="border:none;border-top:1px solid #e2e8f0;margin:24px 0">
              <p style="color:#94a3b8;font-size:12px">Burnout Risk Management &mdash; Hamburgerz</p>
            </div>
            """;
    }
}
