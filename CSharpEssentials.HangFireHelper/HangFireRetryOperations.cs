namespace CSharpEssentials.HangFireHelper;
public class HangFireRetryOperations {
    public int Id { get; set; }
    public string GameSessionID { get; set; }
    public string OperationName { get; set; }
    public string Url { get; set; }
    public string ContentType { get; set; }
    public DateTime CreatedDate { get; set; }
    public string Payload { get; set; }
    public bool MustRetry { get; set; }
    public string Status { get; set; }
    public string BodyResponse { get; set; }
}