using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;

namespace CSharpEssentials.LoggerHelper.Sink.Telegram;
internal class CustomTelegramSink : ILogEventSink {
    private readonly string _botToken;
    private readonly string _chatId;
    private readonly ITextFormatter _formatter;
    private static readonly HttpClient _client = new HttpClient();
    TimeSpan _ThrottleInterval;
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomTelegramSink"/> class.
    /// </summary>
    /// <param name="botToken">The bot token used to authenticate with the Telegram Bot API.</param>
    /// <param name="chatId">The chat ID to which log messages will be sent.</param>
    /// <param name="formatter">Formatter used to convert log events to string messages.</param>
    internal CustomTelegramSink(string botToken, string chatId, ITextFormatter formatter, TimeSpan ThrottleInterval) {
        _botToken = botToken;
        _chatId = chatId;
        _formatter = formatter;
        _ThrottleInterval = ThrottleInterval;
    }
    public void Emit(LogEvent logEvent) {
        using var sw = new StringWriter();
        _formatter.Format(logEvent, sw);
        var message = sw.ToString();

        if (!SinkThrottlingManager.CanSend("Telegram", _ThrottleInterval)) {
            SelfLog.WriteLine($"Throttle exceeded on sink Telegram. Message : {message}");
            return;
        }
        SendMessageAsync(message).Wait();
    }
    /// <summary>
    /// Emits a log event by formatting it and sending it as a Telegram message.
    /// </summary>
    /// <param name="logEvent">The log event to send.</param>
    internal async Task SendMessageAsync(string message) {
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        
        var data = new Dictionary<string, string>
        {
            { "chat_id", _chatId },
            { "text", message },
            { "parse_mode", "MarkdownV2" }
        };

        var TelegramResponse = await _client.PostAsync(url, new FormUrlEncodedContent(data));
        if (TelegramResponse.StatusCode != System.Net.HttpStatusCode.OK) {
            var errorTlg = await TelegramResponse.Content.ReadAsStringAsync();
            SelfLog.WriteLine($"Error on sink Telegram. Message : {errorTlg}");
        }
    }
}