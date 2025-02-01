using System.ComponentModel.DataAnnotations.Schema;

namespace CSharpEssentials.HangFireHelper;
public class HangFireRetryOperations {
    [Column("id")]
    public int Id { get; set; }
    [Column("gamesessionid")]
    public string GameSessionID { get; set; }
    [Column("operationname")]
    public string OperationName { get; set; }
    [Column("url")]
    public string Url { get; set; }
    [Column("contenttype")]
    public string ContentType { get; set; }
    [Column("createddate")]
    public DateTime CreatedDate { get; set; }
    [Column("payload")]
    public string Payload { get; set; }
    [Column("retrycount")]
    public bool MustRetry { get; set; }
    [Column("status")]
    public string Status { get; set; }
    [Column("bodyresponse")]
    public string BodyResponse { get; set; }
}

