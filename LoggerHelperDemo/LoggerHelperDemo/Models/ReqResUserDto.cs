namespace LoggerHelperDemo.Models;
public class ReqResUserDto {
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    public string First_name { get; set; } = default!;
    public string Last_name { get; set; } = default!;
    public string Avatar { get; set; } = default!;
}

public class ReqResResponse {
    public List<ReqResUserDto> Data { get; set; } = new();
}