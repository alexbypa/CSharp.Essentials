using CSharpEssentials.LoggerHelper;

namespace LoggerHelperDemo.Models;
public class LoggerRequest : IRequest {
    public string IdTransaction => Guid.NewGuid().ToString();

    public string Action => "Demo";

    public string ApplicationName => "Demo Logger";
}
