#!/usr/bin/env bash
set -euo pipefail

ARTIFACTS_DIR="benchmark-results/results"
OUTPUT="docs/benchmarks.md"

mkdir -p docs

{
  echo "# LoggerHelper v5 — Benchmark Results"
  echo ""
  echo "> Generated: $(date -u '+%Y-%m-%d') | Runtime: .NET 9 | OS: ubuntu-latest"
  echo ""
  echo "Comparison: **LoggerHelper v5** vs **Serilog** (baseline) vs **NLog**."
  echo "All frameworks use a no-op sink/target — measures framework overhead, not I/O."
  echo ""
  echo "---"
  echo ""
  echo "## Throughput"
  echo ""
  if ls "$ARTIFACTS_DIR"/*ThroughputBenchmark-report-github.md 1>/dev/null 2>&1; then
    cat "$ARTIFACTS_DIR"/*ThroughputBenchmark-report-github.md
  else
    echo "_Results not available._"
  fi
  echo ""
  echo "---"
  echo ""
  echo "## Routing Overhead"
  echo ""
  if ls "$ARTIFACTS_DIR"/*RoutingBenchmark-report-github.md 1>/dev/null 2>&1; then
    cat "$ARTIFACTS_DIR"/*RoutingBenchmark-report-github.md
  else
    echo "_Results not available._"
  fi
  echo ""
  echo "---"
  echo ""
  echo "## Startup Time"
  echo ""
  if ls "$ARTIFACTS_DIR"/*StartupBenchmark-report-github.md 1>/dev/null 2>&1; then
    cat "$ARTIFACTS_DIR"/*StartupBenchmark-report-github.md
  else
    echo "_Results not available._"
  fi
  echo ""
  echo "---"
  echo ""
  echo "_Benchmarks run automatically on each release via [GitHub Actions](../.github/workflows/benchmarks.yml)._"
} > "$OUTPUT"

echo "Generated $OUTPUT"
