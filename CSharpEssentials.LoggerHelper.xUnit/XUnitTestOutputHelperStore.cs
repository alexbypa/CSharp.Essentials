using Xunit.Abstractions;

namespace CSharpEssentials.LoggerHelper.xUnit;

public static class XUnitTestOutputHelperStore {
    public static ITestOutputHelper? Output { get; private set; }

    public static void SetOutput(ITestOutputHelper output) {
        Output = output;
    }

    public static void Clear() {
        Output = null;
    }
}
