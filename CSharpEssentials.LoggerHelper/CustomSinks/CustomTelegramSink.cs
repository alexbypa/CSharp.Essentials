using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace CSharpEssentials.LoggerHelper.CustomSinks;
/// <summary>
/// A custom sink for Serilog that sends log events as messages to a Telegram chat.
/// </summary>
internal class CustomTelegramSink : ILogEventSink
{
    private readonly string _botToken;
    private readonly string _chatId;
    private readonly ITextFormatter _formatter;
    private static readonly HttpClient _client = new HttpClient();
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomTelegramSink"/> class.
    /// </summary>
    /// <param name="botToken">The bot token used to authenticate with the Telegram Bot API.</param>
    /// <param name="chatId">The chat ID to which log messages will be sent.</param>
    /// <param name="formatter">Formatter used to convert log events to string messages.</param>
    internal CustomTelegramSink(string botToken, string chatId, ITextFormatter formatter)
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
    /// <summary>
    /// Emits a log event by formatting it and sending it as a Telegram message.
    /// </summary>
    /// <param name="logEvent">The log event to send.</param>
    internal async Task SendMessageAsync(string message)
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