namespace CSharpEssentials.HangFireHelper;
public class HangFireHttpJobRequest {
    public string GameSessionID { get; set; }
    public string ActionCommand { get; set; }
    public string Url { get; set; }
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
}