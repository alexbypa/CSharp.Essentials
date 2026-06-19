#!/usr/bin/env bash
set -euo pipefail

ARTIFACTS_DIR="benchmark-results/results"
OUTPUT="docs/benchmarks.md"

mkdir -p docs

echo "=== Searching for benchmark results in $ARTIFACTS_DIR ==="
find benchmark-results -name "*.md" -type f 2>/dev/null || echo "No .md files found"

append_section() {
  local title="$1"
  local pattern="$2"
  echo ""
  echo "## $title"
  echo ""
  if ls "$ARTIFACTS_DIR"/*${pattern}*-report-github.md 1>/dev/null 2>&1; then
    cat "$ARTIFACTS_DIR"/*${pattern}*-report-github.md
  else
    echo "_Results not available._"
  fi
  echo ""
  echo "---"
}

{
  echo "# LoggerHelper v5 — Benchmark Results"
  echo ""
  echo "> Generated: $(date -u '+%Y-%m-%d') | Runtime: .NET 9 | OS: ubuntu-latest"
  echo ""
  echo "Comparison: **LoggerHelper v5** vs **Serilog** (baseline) vs **NLog**."
  echo "All frameworks use a no-op sink/target — measures framework overhead, not I/O."
  echo ""
  echo "---"
  append_section "Throughput" "ThroughputBenchmark"
  append_section "Routing Overhead" "RoutingBenchmark"
  append_section "Startup Time" "StartupBenchmark"
  append_section "Emit Overhead" "EmitOverheadBenchmark"
  append_section "Sink Routing Match" "SinkRoutingMatchBenchmark"
  append_section "Sensitive Data Masking" "SensitiveDataMaskingBenchmark"
  append_section "MCP Tools" "McpToolsBenchmark"
  append_section "Sampling" "SamplingSinkBenchmark"
  echo ""
  echo "_Benchmarks run automatically on each release via [GitHub Actions](../.github/workflows/benchmarks.yml)._"
} > "$OUTPUT"

echo "Generated $OUTPUT"
