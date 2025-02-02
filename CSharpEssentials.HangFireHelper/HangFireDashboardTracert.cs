using Hangfire.Console;
using Hangfire.Server;

namespace CSharpEssentials.HangFireHelper;
public class HangFireDashboardTracert {
    public delegate void WriteTextOnDashboardHandler(byte levelLog, PerformContext performcontext, WriteTextOnDashboard e);
    public event WriteTextOnDashboardHandler OnWriteText;
    protected virtual void RaiseMessage(byte levelLog, string message, PerformContext performcontext) {
        switch (levelLog) {
            case 1:
                performcontext.SetTextColor(ConsoleTextColor.Gray);
                break;
            case 2:
                performcontext.SetTextColor(ConsoleTextColor.Yellow);
                break;
            case 3:
                performcontext.SetTextColor(ConsoleTextColor.Red);
                break;
            default:
                performcontext.SetTextColor(ConsoleTextColor.DarkRed);
                break;
        }
        OnWriteText.Invoke(levelLog, performcontext, new WriteTextOnDashboard(message));
        performcontext.ResetTextColor();
    }
}