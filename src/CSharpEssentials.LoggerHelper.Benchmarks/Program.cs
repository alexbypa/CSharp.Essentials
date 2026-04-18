using BenchmarkDotNet.Running;
using CSharpEssentials.LoggerHelper.Benchmarks;

// Usage:
//   Full run (accurate, heavy):    dotnet run -c Release --framework net9.0 -- --filter *
//   Light run (fast, low CPU):     dotnet run -c Release --framework net9.0 -- --filter * --job short
//   Single class only:             dotnet run -c Release --framework net9.0 -- --filter *Throughput*
//   Light + single class:          dotnet run -c Release --framework net9.0 -- --filter *Routing* --job short
//   Memory leak soak test (30m):   dotnet run -c Release --framework net9.0 -- --leak-test
//   Memory leak soak test (5m):    dotnet run -c Release --framework net9.0 -- --leak-test --duration 5

if (args.Contains("--leak-test")) {
    int duration = 30;
    int durationIdx = Array.IndexOf(args, "--duration");
    if (durationIdx >= 0 && durationIdx + 1 < args.Length)
        int.TryParse(args[durationIdx + 1], out duration);

    return await MemoryLeakTest.RunAsync(duration);
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
return 0;
