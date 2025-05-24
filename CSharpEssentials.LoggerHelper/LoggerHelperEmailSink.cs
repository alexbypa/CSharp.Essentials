using Serilog.Core;
using Serilog.Events;
using System.Net;
using System.Net.Mail;

namespace CSharpEssentials.LoggerHelper;
public class LoggerHelperEmailSink : ILogEventSink {
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _fromEmail;
    private readonly string _toEmail;
    private readonly NetworkCredential _credentials;
    private readonly string _subjectPrefix;
    private readonly bool _enableSsl;
    private readonly string _templatePath;
    private readonly string _defaultTemplate;

    public LoggerHelperEmailSink(
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


    //    private string GenerateHtmlBody(LogEvent logEvent) {
    //        var rawMessage = logEvent.RenderMessage();
    //        var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    //        var level = logEvent.Level.ToString();
    //        var levelClass = GetLevelColorClass(logEvent.Level);

    //        var idTransaction = ExtractProperty(logEvent, "IdTransaction");
    //        var action = ExtractProperty(logEvent, "Action");
    //        var appName = ExtractProperty(logEvent, "ApplicationName");
    //        var machineName = ExtractProperty(logEvent, "MachineName");
    //        return $@"
    //<html>
    //<head>
    //  <style>
    //    body {{ font-family: Arial, sans-serif; margin: 20px; }}
    //    .header {{ font-size: 24px; font-weight: bold; color: green; }}
    //    .section {{ margin-top: 20px; }}
    //    .label {{ font-weight: bold; color: #555; }}
    //    .value {{ margin-bottom: 10px; }}
    //    .level-info {{ color: green; font-weight: bold; }}
    //    .level-warning {{ color: orange; font-weight: bold; }}
    //    .level-error {{ color: red; font-weight: bold; }}
    //    table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
    //    td, th {{ border: 1px solid #ddd; padding: 8px; }}
    //    th {{ background-color: #f2f2f2; }}
    //    .highlight {{ background-color: #e8f5e9; padding: 8px; border-radius: 5px; }}
    //  </style>
    //</head>
    //<body>

    //<div class='header'>LoggerHelper Notification 🚀</div>

    //<div class='section'>
    //  <span class='label'>Timestamp:</span> <span class='value'>{timestamp}</span><br/>
    //  <span class='label'>Level:</span> <span class='value {levelClass}'>{level}</span><br/>
    //</div>

    //<div class='section'>
    //  <h3>Core Details</h3>
    //  <table>
    //    <tr><th>IdTransaction</th><td>{idTransaction}</td></tr>
    //    <tr><th>Action</th><td>{action}</td></tr>
    //    <tr><th>ApplicationName</th><td>{appName}</td></tr>
    //    <tr><th>MachineName</th><td>{machineName}</td></tr>
    //  </table>
    //</div>

    //<div class='section highlight'>
    //  <h3>Log Message</h3>
    //  <pre>{WebUtility.HtmlEncode(rawMessage)}</pre>
    //</div>
    //</body>
    //</html>";
    //    }
    private string ExtractProperty(LogEvent logEvent, string propertyName) {
        if (logEvent.Properties.TryGetValue(propertyName, out var value)) {
            return value.ToString().Trim('"');
        }
        return "";
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
