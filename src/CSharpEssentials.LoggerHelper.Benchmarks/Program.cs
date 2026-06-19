using BenchmarkDotNet.Running;
using CSharpEssentials.LoggerHelper.Benchmarks;

// Usage:
//   CI run (fast, GitHub export):   dotnet run -c Release -- --filter *
//   Single class only:              dotnet run -c Release -- --filter *Throughput*
//   Memory leak soak test (30m):    dotnet run -c Release -- --leak-test
//   Memory leak soak test (5m):     dotnet run -c Release -- --leak-test --duration 5

if (args.Contains("--leak-test")) {
    int duration = 30;
    int durationIdx = Array.IndexOf(args, "--duration");
    if (durationIdx >= 0 && durationIdx + 1 < args.Length)
        int.TryParse(args[durationIdx + 1], out duration);

    return await MemoryLeakTest.RunAsync(duration);
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new CIConfig());
return 0;
