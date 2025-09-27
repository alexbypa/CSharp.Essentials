# CSharpEssentials.LoggerHelper.AI: AI-Powered Observability & Root Cause Analysis

🚀 **Stop Sifting, Start Solving.**

Are you tired of manually sifting through mountains of logs and scattered traces? **CSharpEssentials.LoggerHelper.AI** is an advanced toolkit that integrates **Large Language Models (LLMs)** and **Retrieval-Augmented Generation (RAG)** directly into your C# logging pipeline. It transforms raw operational data into actionable, real-time insights, dramatically reducing Mean Time To Resolution (MTTR).

## Key AI Actions (Four Modes)

This powerful package offers specialized macro actions designed to handle complex observability challenges:

1.  **🕵️ CorrelateTrace:** Automatically analyze distributed traces to pinpoint the most suspicious span or root cause of timeouts and complex failures in microservice architectures.
2.  **❓ RagAnswerQuery:** Use RAG over your database logs/metrics (via dynamic SQL queries) to get precise answers to natural language questions instantly.
3.  **🚨 DetectAnomaly:** Analyze time-series metrics (e.g., latency, error rates) to detect statistical anomalies (Z-score analysis) and provide an AI-driven explanation of the root cause and mitigation steps.
4.  **📚 SummarizeIncident:** Generate concise summaries of an entire incident, detailing the root cause, impact, and remediation based on a full chronological timeline of associated logs.

## Hybrid Contextual Data Sourcing (Unique Feature)

The package securely enriches LLM prompts by dynamically executing pre-saved SQL queries (`.sql` files) against your existing database, **supporting both MS SQL and PostgreSQL**. This flexibility allows you to seamlessly bridge the gap between your operational data and advanced AI analysis.

---

## Get Started

| Resource | Description |
| :--- | :--- |
| **Documentation** | View the detailed setup guide and technical deep-dive here: [doc.md](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper.AI/doc.md) |
| **Web Demo** | Test the API endpoints and core functionality immediately using the web demo project: [CSharp.Essentials.Extensions](https://github.com/alexbypa/Csharp.Essentials.Extensions) |
| **LLM Dashboard** | Manage and test all four LLM modes dynamically with a user-friendly UI using the companion package: [CSharpEssentials.LoggerHelper.Dashboard](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Dashboard) |

**Start transforming your observability with AI today!**