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
                if (name == "SinkThrottlingManagerTests" || name == "HttpExtensionTests") continue;
                
                var content = File.ReadAllText(file);
                
                // Match methods with [Fact] or [Theory]
                var methodRegex = new Regex(@"(\[(?:Fact|Theory).*?\]\s*(?:\[.*?\]\s*)*public\s+(?:async\s+Task|void|Task)\s+)(\w+)(\(.*?\)\s*\{)", RegexOptions.Singleline);
                
                content = methodRegex.Replace(content, match => {
                    var fullMatch = match.Groups[0].Value;
                    var prefix = match.Groups[1].Value;
                    var methodName = match.Groups[2].Value;
                    var suffix = match.Groups[3].Value;
                    
                    if (fullMatch.Contains("?? [Scenario]")) return fullMatch; // Already processed
                    
                    var parts = methodName.Split('_');
                    string scenario = methodName;
                    string expected = "Verifica dei risultati attesi";
                    
                    if (parts.Length >= 2) {
                        scenario = string.Join(" ", parts.Take(parts.Length - 1).Select(p => Regex.Replace(p, "([A-Z])", " $1").Trim()));
                        expected = Regex.Replace(parts.Last(), "([A-Z])", " $1").Trim();
                    }
                    
                    var insertion = $"\r\n        _output.WriteLine(\"?? [Scenario] Test: {scenario}\");\r\n";
                    return prefix + methodName + suffix + insertion;
                });
                
                // Try to inject before the first Assert
                var assertRegex = new Regex(@"(\r\n\s*)(Assert\.)");
                bool firstAssert = true;
                content = assertRegex.Replace(content, match => {
                    if (firstAssert) {
                        // firstAssert = false; // We can't do stateful easily in Regex.Replace without a MatchEvaluator that resets per method.
                        // We will just do a simpler approach: replace all Assert. with a prepended log if it doesn't already have one.
                    }
                    return match.Groups[0].Value;
                });
                
                File.WriteAllText(file, content);
                Console.WriteLine("Updated " + name);
            }
        }
    }
}
