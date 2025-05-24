using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace CSharpEssentials.LoggerHelper.CustomSinks;
public class CustomTelegramSink : ILogEventSink
{
    private readonly string _botToken;
    private readonly string _chatId;
    private readonly ITextFormatter _formatter;
    private static readonly HttpClient _client = new HttpClient();

    public CustomTelegramSink(string botToken, string chatId, ITextFormatter formatter)
    {
        _botToken = botToken;
        _chatId = chatId;
        _formatter = formatter;
    }

    public void Emit(LogEvent logEvent)
    {
        using var sw = new StringWriter();
        _formatter.Format(logEvent, sw);
        var message = sw.ToString();

        SendMessageAsync(message).Wait();
    }

    private async Task SendMessageAsync(string message)
    {
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

        var data = new Dictionary<string, string>
        {
            { "chat_id", _chatId },
            { "text", message },
            { "parse_mode", "MarkdownV2" }
        };

        await _client.PostAsync(url, new FormUrlEncodedContent(data));
    }
}
