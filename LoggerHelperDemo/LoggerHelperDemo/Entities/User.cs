namespace LoggerHelperDemo.Entities;
public class User {
    public int Id { get; set; }             // PK locale
    public int ExternalId { get; set; }     // id da reqres
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Avatar { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
