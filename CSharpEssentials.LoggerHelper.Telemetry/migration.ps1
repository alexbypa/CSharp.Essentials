param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString
)

dotnet ef database update `
  --project D:\Project_Pixelo\CSharp.Essentials\CSharpEssentials.LoggerHelper.Telemetry\CSharpEssentials.LoggerHelper.Telemetry.csproj `
  --startup-project D:\github\Csharp.Essentials.Extensions\Web.Api\Web.Api.csproj `
  --connection "$ConnectionString" `
  --context TelemetryDbContextSqlServer