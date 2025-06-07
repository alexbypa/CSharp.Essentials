using Serilog.Core;
using Serilog.Events;
using System.Net;
using System.Net.Mail;

namespace CSharpEssentials.LoggerHelper.Sink.Email;
internal class CustomEmailSink : ILogEventSink {
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _fromEmail;
    private readonly string _toEmail;
    private readonly NetworkCredential _credentials;
    private readonly string _subjectPrefix;
    private readonly bool _enableSsl;
    private readonly string _templatePath;
    private readonly string _defaultTemplate;
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEmailSink"/> class.
    /// </summary>
    /// <param name="smtpServer">SMTP server address.</param>
    /// <param name="smtpPort">SMTP server port.</param>
    /// <param name="fromEmail">Sender email address.</param>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="username">Username for SMTP authentication.</param>
    /// <param name="password">Password for SMTP authentication.</param>
    /// <param name="subjectPrefix">Subject prefix for the email.</param>
    /// <param name="enableSsl">Whether to use SSL for SMTP.</param>
    /// <param name="templatePath">Path to the HTML template file.</param>
    public CustomEmailSink(
        string smtpServer,
        int smtpPort,
        string fromEmail,
        string toEmail,
        string username,
        string password,
        string subjectPrefix = "[LoggerHelper]",
        bool enableSsl = true,
        string templatePath = "email-template-default.html") {
        _smtpServer = smtpServer;
        _smtpPort = smtpPort;
        _fromEmail = fromEmail;
        _toEmail = toEmail;
        _credentials = new NetworkCredential(username, password);
        _subjectPrefix = subjectPrefix;
        _enableSsl = enableSsl;
        _templatePath = templatePath;
        _defaultTemplate = LoadDefaultTemplate(); // embedded di backup
    }
    /// <summary>
    /// Emits the log event by sending it as an email using the configured SMTP settings.
    /// </summary>
    /// <param name="logEvent">The log event to send.</param>
    public void Emit(LogEvent logEvent) {
        try {
            var rawMessage = logEvent.RenderMessage();
            var htmlBody = GenerateHtmlBody(logEvent);

            var subject = $"{_subjectPrefix} [{logEvent.Level}]";

            using var message = new MailMessage(_fromEmail, _toEmail, subject, htmlBody);
            message.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(_smtpServer, _smtpPort) {
                EnableSsl = _enableSsl,
                Credentials = _credentials
            };

            smtpClient.Send(message);
        } catch (Exception ex) {
            // Qui puoi decidere se loggare errori di invio o ignorarli
            Serilog.Debugging.SelfLog.WriteLine($"Error sending email: {ex}");
        }
    }
    /// <summary>
    /// Generates the HTML email body based on the log event and the template.
    /// </summary>
    /// <param name="logEvent">The log event to include in the email.</param>
    /// <returns>The HTML body of the email.</returns>
    private string GenerateHtmlBody(LogEvent logEvent) {
        string template;
        if (!string.IsNullOrWhiteSpace(_templatePath) && File.Exists(_templatePath)) {
            template = File.ReadAllText(_templatePath);
        } else {
            template = _defaultTemplate; // embedded fallback
        }
        string rawMessage = logEvent.RenderMessage();
        string timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        string level = logEvent.Level.ToString();
        string levelClass = GetLevelColorClass(logEvent.Level);


        // Base replacements
        template = template
            .Replace("{{Timestamp}}", timestamp)
            .Replace("{{Level}}", level)
            .Replace("{{LevelClass}}", levelClass)
            .Replace("{{Message}}", WebUtility.HtmlEncode(rawMessage));

        // Dynamic replacements for any {{PropertyName}} found in the template
        foreach (var prop in logEvent.Properties) {
            template = template.Replace($"{{{{{prop.Key}}}}}", WebUtility.HtmlEncode(prop.Value?.ToString()));
        }

        return template;
    }
    /// <summary>
    /// Loads the default HTML template embedded in the code as a fallback.
    /// </summary>
    /// <returns>The default HTML email template.</returns>
    private string LoadDefaultTemplate() {
        return @"
<html>
<head>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; }
    .header { font-size: 24px; font-weight: bold; color: green; }
    .section { margin-top: 20px; }
    .label { font-weight: bold; color: #555; }
    .value { margin-bottom: 10px; }
    .level-info { color: green; font-weight: bold; }
    .level-warning { color: orange; font-weight: bold; }
    .level-error { color: red; font-weight: bold; }
    table { width: 100%; border-collapse: collapse; margin-top: 10px; }
    td, th { border: 1px solid #ddd; padding: 8px; }
    th { background-color: #f2f2f2; }
    .highlight { background-color: #e8f5e9; padding: 8px; border-radius: 5px; }
  </style>
</head>
<body>
  <div class='header'>LoggerHelper Notification 🚀</div>
  <div class='section'>
    <span class='label'>Timestamp:</span> <span class='value'>{{Timestamp}}</span><br/>
    <span class='label'>Level:</span> <span class='value {{LevelClass}}'>{{Level}}</span><br/>
  </div>

  <div class='section'>
    <h3>Core Details</h3>
    <table>
      <tr><th>IdTransaction</th><td>{{IdTransaction}}</td></tr>
      <tr><th>Action</th><td>{{Action}}</td></tr>
      <tr><th>ApplicationName</th><td>{{ApplicationName}}</td></tr>
      <tr><th>MachineName</th><td>{{MachineName}}</td></tr>
    </table>
  </div>

  <div class='section highlight'>
    <h3>Log Message</h3>
    <pre>{{Message}}</pre>
  </div>
</body>
</html>";
    }
    private string GetLevelColorClass(LogEventLevel level) {
        return level switch {
            LogEventLevel.Information => "level-info",
            LogEventLevel.Warning => "level-warning",
            LogEventLevel.Error => "level-error",
            _ => ""
        };
    }
}