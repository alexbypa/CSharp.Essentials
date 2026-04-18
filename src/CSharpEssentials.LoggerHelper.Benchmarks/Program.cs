using BenchmarkDotNet.Running;
using CSharpEssentials.LoggerHelper.Benchmarks;

// Usage:
//   Full run (accurate, heavy):    dotnet run -c Release --framework net9.0 -- --filter *
//   Light run (fast, low CPU):     dotnet run -c Release --framework net9.0 -- --filter * --job short
//   Single class only:             dotnet run -c Release --framework net9.0 -- --filter *Throughput*
//   Light + single class:          dotnet run -c Release --framework net9.0 -- --filter *Routing* --job short
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
