namespace CSharpEssentials.HangFireHelper;
public class HangfireOptions {
    public Dashboard Dashboard { get; set; }
    public string ConnectionString { get; set; }
}
public class Dashboard {
    public string RelativePath { get; set; }
    public string IpAuthorized { get; set; }
    public bool IsReadOnly { get; set; }
    public List<AuthorizationHangFire> AuthorizationHangFire { get; set; }
}
public class AuthorizationHangFire {
    public string UserName { get; set; }
    public string Password { get; set; }
    public bool isReadOnly { get; set; }
    public string IpAuthorized { get; set; }
}