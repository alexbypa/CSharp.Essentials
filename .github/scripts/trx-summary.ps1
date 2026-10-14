# Renders a TRX file as a Markdown/HTML report with the per-test output (ITestOutputHelper),
# which dorny/test-reporter and GitHubActionsTestLogger do not show for passed tests.
# One collapsible section per test class; tests that wrote output or failed get a nested section.
# Usage (pwsh): pwsh -File .github/scripts/trx-summary.ps1 -Path <file.trx> >> $env:GITHUB_STEP_SUMMARY
param([Parameter(Mandatory)][string]$Path)

function Esc([string]$s) { [System.Net.WebUtility]::HtmlEncode($s) }
# The TRX logger writes each half of a surrogate pair (emoji outside the BMP) as a literal \uXXXX: decode the pairs back.
function Unescape([string]$s) {
  [regex]::Replace($s, '\\u(d[89ab][0-9a-f]{2})\\u(d[c-f][0-9a-f]{2})', {
    param($m) [char]::ConvertFromUtf32([char]::ConvertToUtf32([char][Convert]::ToInt32($m.Groups[1].Value, 16), [char][Convert]::ToInt32($m.Groups[2].Value, 16)))
  }, 'IgnoreCase')
}
function Icon([string]$outcome) {
  switch ($outcome) { 'Passed' { '✅' } 'Failed' { '❌' } default { '⚠️' } }
}

[xml]$trx = Get-Content $Path -Raw
$classOf = @{}
foreach ($t in $trx.TestRun.TestDefinitions.UnitTest) { $classOf[$t.id] = $t.TestMethod.className }

$byClass = $trx.TestRun.Results.UnitTestResult |
  Group-Object { $classOf[$_.testId] } | Sort-Object Name

"## Test output"
""
foreach ($g in $byClass) {
  $results = $g.Group | Sort-Object testName
  $failed = @($results | Where-Object outcome -eq 'Failed').Count
  $icon = if ($failed) { '❌' } else { '✅' }
  "<details><summary>$icon <b>$(Esc $g.Name)</b> ($($results.Count) tests, $failed failed)</summary>"
  ""
  "<ul>"
  foreach ($r in $results) {
    $name = $r.testName
    if ($name.StartsWith("$($g.Name).")) { $name = $name.Substring($g.Name.Length + 1) }
    $out = Unescape $r.Output.StdOut
    $err = $r.Output.ErrorInfo
    $line = "$(Icon $r.outcome) $(Esc $name) <sub>$($r.duration)</sub>"
    if (-not $out -and -not $err) { "<li>$line</li>"; continue }
    "<li><details><summary>$line</summary>"
    if ($out) { "<pre>$(Esc $out.Trim())</pre>" }
    if ($err) { "<pre>$(Esc ("$($err.Message)`n$($err.StackTrace)".Trim()))</pre>" }
    "</details></li>"
  }
  "</ul>"
  "</details>"
  ""
}
