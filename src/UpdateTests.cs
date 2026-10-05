using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

class Program {
    static void Main() {
        var dirs = new[] {
            @"d:\Work\CSharp.Essentials.Private\CSharp.Essentials.Private\src\CSharpEssentials.LoggerHelper.Tests",
            @"d:\Work\CSharp.Essentials.Private\CSharp.Essentials.Private\src\CSharpEssentials.HttpHelper.Tests"
        };
        
        foreach (var dir in dirs) {
            foreach (var file in Directory.GetFiles(dir, "*.cs")) {
                var name = Path.GetFileNameWithoutExtension(file);
                var content = File.ReadAllText(file);
                if (content.Contains("ITestOutputHelper")) continue;
                
                // Add using if not exists
                if (!content.Contains("using Xunit.Abstractions;")) {
                    content = "using Xunit.Abstractions;\r\n" + content;
                }
                
                // Check if constructor exists
                var ctorRegex = new Regex(@"public\s+" + name + @"\s*\((.*?)\)\s*\{");
                var match = ctorRegex.Match(content);
                
                var classRegex = new Regex(@"(public\s+(sealed\s+)?class\s+" + name + @"(?:\s*:\s*[^{]+)?\s*\{)");
                
                if (match.Success) {
                    // Update existing constructor
                    var args = match.Groups[1].Value;
                    var newArgs = string.IsNullOrWhiteSpace(args) ? "ITestOutputHelper output" : args + ", ITestOutputHelper output";
                    
                    content = ctorRegex.Replace(content, "public " + name + "(" + newArgs + ") {\r\n        _output = output;", 1);
                    
                    // Add field
                    content = classRegex.Replace(content, "\r\n    private readonly ITestOutputHelper _output;\r\n", 1);
                } else {
                    // Add field and constructor
                    var replacement = "\r\n    private readonly ITestOutputHelper _output;\r\n\r\n    public " + name + "(" + "ITestOutputHelper output" + ") {\r\n        _output = output;\r\n    }\r\n";
                    content = classRegex.Replace(content, replacement, 1);
                }
                
                File.WriteAllText(file, content);
                Console.WriteLine("Updated " + name);
            }
        }
    }
}
