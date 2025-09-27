# CSharpEssentials.LoggerHelper.AI: Advanced AI Analysis Toolkit (RAG, Correlate Trace, Summarize Incident, Detect Anomaly)

🚀 **The Challenge: Log Analysis Is Too Slow?**

Are you tired of manually sifting through mountains of logs and connecting scattered traces to understand complex failures?
**CSharpEssentials.LoggerHelper.AI** offers a powerful solution by integrating **Large Language Models (LLMs)** directly into your logging and observability pipeline. It transforms raw log data and operational context into **actionable, real-time insights**.

---

## AI Package Configuration

To enable AI features in your project, you must add the `LoggerAIOptions` section to your `AppSettings.json` file. This section contains all the necessary settings to configure the AI model and its connection to the service.

```json
"LoggerAIOptions": {
  "Model": "gpt-4o-mini",
  "chatghapikey": "github_pat_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "FolderSqlLoaderContainer": "D:\\github\\Csharp.Essentials.Extensions\\Web.Api\\SqlQueries",
  "Temperature": 0.7,
  "topScore": 5,
  "urlLLM": "https://models.inference.ai.azure.com/chat/completions",
  "headersLLM": [
    { "accept": "application/json" },
    { "X-GitHub-Api-Version": "2023-10-01" }
  ],
  "httpClientName": "testAI"
}
```

### ⚠️ Configuration Error Diagnostics

Should you encounter configuration issues with your `appSettings.json` file, don't worry. The `CSharpEssentials.LoggerHelper.AI` logger instance is designed to send an **error message** directly to your dashboard.

In the **Monitor-Sink** section, by filtering the `Sink` for "LoggerHelper.AI," you will be able to view the error details. This allows you to quickly identify and resolve any missing or invalid configuration keys.

![Dashboard AI Errors](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper.AI/Docs/Dashboard_AI_Error.png)

---

### Key Settings

*   **`Model`**: Specifies the name of the AI model to be used (e.g., `gpt-4o-mini`), which must be available on the configured AI inference service.
*   **`chatghapikey`**: Your GitHub API key, used for authentication with the chat service.
*   **`FolderSqlLoaderContainer`**: Defines the local path where SQL query files for the AI are stored. The AI assistant uses these queries to interact with your data.
*   **`Temperature`**: Controls the randomness of the AI model's output. Lower values produce more deterministic and focused responses, while higher values lead to more creative and varied outputs. The value should be between 0.0 and 1.0.
*   **`topScore`**: A parameter used for search results. It determines the number of top-ranking results to be considered by the AI model when retrieving information.
*   **`urlLLM`**: The URL endpoint of the Large Language Model (LLM) service. This is the address where API requests are sent.
*   **`headersLLM`**: A collection of HTTP headers required for API requests, such as authentication tokens or content types.
*   **`httpClientName`**: The name of the `HttpClient` instance used for making requests to the LLM service. This is typically configured in the application's startup file.

---

### Managing HttpClients with `CSharpEssentials.HttpHelper`

`httpClientName` specifies the name of the `HttpClient` used for making requests to the Large Language Model (LLM) service. This name corresponds to a named client configured in your application's startup file, typically using the `CSharpEssentials.HttpHelper` NuGet package.

By using a **named `HttpClient`**, you can centralize the configuration for the LLM service, including the base URL and any required headers (like those for authentication). This approach offers several benefits:

*   **Reusability**: You can reuse the same client configuration across different parts of your application without repeating code.
*   **Centralized Control**: All client-specific settings, such as timeouts and request headers, are managed in one place.
*   **Testability**: Named clients simplify testing by allowing you to easily mock or substitute the `HttpClient` for unit tests.

The `CSharpEssentials.HttpHelper` package streamlines this process by providing an easy way to define and manage these named clients, ensuring consistency and maintainability in your codebase.

---

### 🌟 Hybrid Contextual Data Sourcing (Unique Feature!)

`FolderSqlLoaderContainer` defines the path where SQL query files for the AI are stored. These files provide the context for the AI's actions, allowing it to perform specific tasks. The system currently supports four distinct modes, each corresponding to a different action.

**Example Folder Structure for `FolderSqlLoaderContainer`:**

```
YourApp/SqlQueries/
├── RagAnswerQuery/
│   ├── getLogs.sql
│   └── getLogs.txt
│   ├── getTraces.sql
│   └── getTraces.txt
├── CorrelateTrace/
│   ├── getTraces.sql
│   └── getTraces.txt
├── SummarizeIncident/
│   ├── getLogsForIncident.sql
│   └── getLogsForIncident.txt
└── DetectAnomaly/
    ├── getMetrics.sql
    └── getMetrics.txt
```

Enable the AI to reason over your *operational data* by dynamically providing context via a simple folder structure:

*   **SQL Query Injection (`.sql`):** Define your contextual data extraction in standard `.sql` files within a designated folder (`ContextFolderPath`). The AI system dynamically loads and executes these queries.
    
    **Crucially, the SQL syntax in these files must be compatible with the database provider selected in your `appsettings.json`:**
    
    ```json
    {
      "DatabaseProvider": "postgresql", // Set to "postgresql" or "sqlserver"
      "ConnectionStrings": {
        "Default": "Your_Connection_String_Here"
      }
    }
    ```
    
    The selection via `"DatabaseProvider"` determines the dialect and parameter syntax required for your `.sql` files. These queries fetch relevant data (e.g., transaction details, user history) to enrich the LLM prompt.

*   **Structured Formatting and Prompting (`.txt`):** For **every** `.sql` file that defines an extraction, a corresponding file with the **same name and a `.txt` extension must exist**. This `.txt` file dictates the exact **structured format** required for the resulting SQL data. This mechanism ensures the output is perfectly prepared, structured, and optimized to be consumed as context by the LLM, maintaining prompt integrity.

Each mode is represented by a specific C# class that inherits from `ILogMacroAction` and performs a unique task, often using an embedded SQL file to query data.

---

### The Four Available AI Actions

#### 1. `RagAnswerQuery`
This mode is designed to answer user questions based on a specific set of data. It uses **Retrieval-Augmented Generation (RAG)** by fetching the most relevant documents from a vector store and using them as context for the LLM.

*   **How it works**: The system embeds the user's query into a vector and uses it to retrieve similar documents from the vector store. The SQL query defined in the `sqlQuery` variable is used to query the vector store. The retrieved documents are then used as the `CONTEXT` for a prompt, and the AI generates a precise and concise answer.

#### 2. `CorrelateTrace`
This mode helps to identify the most suspicious trace within a recent set of logs.

*   **How it works**: It fetches the most recent 50 traces. It then composes a list of candidate traces, including details like `TraceId`, `duration`, and `anomaly` status. This list is provided to the LLM as a prompt, and the AI is instructed to select the most suspicious one and explain why.

#### 3. `SummarizeIncident`
This action is used to summarize the root cause, impact, and remediation of a specific incident using a `TraceId`.

*   **How it works**: The system fetches up to 200 logs associated with the provided `TraceId`. It builds a compact, chronological timeline from these logs, ensuring the content fits within a specified character budget. This timeline is then passed to the LLM, which generates a concise summary of the incident. The prompt includes an example of the desired output to guide the AI's tone and structure.

#### 4. `DetectAnomaly`
This mode is designed to detect anomalies in a time series of metrics.

*   **How it works**: It queries a repository for a specific metric (e.g., `http.client.request.duration`) over a defined time period (e.g., the last 30 minutes). It then calculates the statistical **mean** and **standard deviation** of the data points. Finally, it computes the **Z-score** of the last data point and determines if it indicates an anomaly based on a predefined threshold (e.g., `z >= 3`).

---

### How to Test AI Models

You can test the four AI actions using two different methods: the [web API demo with Scalar](https://github.com/alexbypa/Csharp.Essentials.Extensions/blob/main/Web.Api/MinimalApi/Endpoints/AI/ApiAIHelperDemo.cs) or the [CSharpEssentials.LoggerHelper.Dashboard](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Dashboard) client application.

#### 1. Web API Demo with Scalar

A web API demo is available to test the AI actions directly. This method allows you to interact with the backend API without using the frontend dashboard. The API documentation can be accessed via the Scalar interface.

To run a test, you must send a `POST` request to the `http://localhost:1234/AI/run` endpoint with a JSON body.

*   **Example for the `RagAnswerQuery` Action:**
    *   **Endpoint:** `http://localhost:1234/AI/run`
    *   **Body:**
        ```json
        {
          "docId": null,
          "traceId": null,
          "query": "I received http response with httpstatus 401",
          "system": "If you don't find anything in the context, reply with 'I'm sorry but I couldn't find this information in the database!'",
          "action": "RagAnswerQuery",
          "fileName": "getTraces.sql",
          "dtStart": "2025-09-22T08:00:00",
          "topResultsOnQuery" : 200
        }
        ```
*   **Result:** The `RagAnswerQuery` action will search for relevant information based on the provided `query` and `fileName`. As shown in the example, if the information is not found in the database, the AI will respond with the message defined in the `system` parameter.

---

#### 2. LoggerHelper.Dashboard

You can also test the AI models directly from the `LoggerHelper.Dashboard` client application. This method provides a user-friendly interface for interacting with the AI.

*   **Interface:** The interface includes dropdown menus for `Action` and `File Name`, along with text areas for `Query` and `System` prompts.
*   **Workflow:**
    1.  Select the desired **`Action`** from the dropdown menu (e.g., `RagAnswerQuery`).
    2.  Choose the specific SQL file from the **`File Name`** dropdown. This file is located in the `FolderSqlLoaderContainer` and corresponds to the selected action (e.g., `getTraces.sql` for `RagAnswerQuery`).
    3.  Enter your **`Query`** and a custom **`System`** prompt to guide the AI's response.
    4.  Click **`Send to LLM`** to process the request.

---

### 📊 RAG with SQL Query Files

A key feature of the **AI Assistant** is its ability to perform **Retrieval-Augmented Generation (RAG)** using pre-saved SQL queries. This is useful for fetching specific data from your database to provide context for the LLM.

#### **Use Case: Analyzing Recent Logs**

This example demonstrates how to use the RAG system to analyze recent log entries by referencing a saved SQL query file.

1.  **Prepare Your SQL Query**:
    Save your query in a `.sql` file within the `RagAnswerQuery` folder. For this example, let's use `getlogs.sql`. The query uses placeholders `{now}` for the current timestamp and `{n}` for the number of results, which are dynamically replaced at runtime.

    Here's an example of the `getlogs.sql` file content:

    ```sql
    select "Id", "ApplicationName" "App", "TimeStamp" "Ts", "Message", "IdTransaction" "TraceId" from "LogEntry"  
    where "TimeStamp" > @now
    order by "Id" desc
    limit @n
    ```

    *   `{now}`: The starting date from the user input. The query will return logs from this date onward.
    *   `{n}`: The limit on the number of results to fetch, which you can specify in the UI.

2.  **Use the AI Assistant Dashboard**:
    Navigate to the **AI Assistant** page in your dashboard.
    *   **Action**: Select `RagAnswerQuery`.
    *   **File Name**: Choose the `getlogs.sql` file.
    *   **Query**: Enter your natural language question, for example: "Were there any HTTP responses with status 401?"
    *   **Start Date**: Set the date to filter the query.
    *   **System**: Add any specific instructions for the LLM, such as "Stick closely to the context. If you don't find anything, reply with 'Sorry but I didn't find anything'".

The system will execute the `getlogs.sql` query, retrieve the relevant log entries, and use that data as context to generate a precise answer to your question.

---

### 🤖 AI-Powered Log Analysis with RAG

A key benefit of the **AI Assistant** is its ability to perform **Retrieval-Augmented Generation (RAG)** on your application's logs, saving operators from having to manually query the database. The LLM can analyze log data provided by a SQL query to answer complex questions instantly.

#### **Example: Troubleshooting an Error**

Let's illustrate with an example. An operator needs to check if a specific HTTP error occurred in the logs.

1.  **Context from SQL Query**:
    The system uses a pre-saved query, like `getlogs.sql`, to retrieve a specific set of log records. The query includes parameters for the start date and the maximum number of records, allowing the operator to define the search scope directly from the dashboard.

2.  **LLM Analysis**:
    The operator inputs a natural language query like "**Are there any HTTP responses with status 401?**" The LLM then receives the results from the executed SQL query as its context. By analyzing this context, the LLM provides a precise and detailed answer, summarizing the findings without the operator ever needing to write SQL or connect to the database.

3.  **Efficiency**:
    This process is extremely efficient. The more logs you write, the more powerful the LLM becomes in providing deep insights into your application's behavior. It automates the task of searching through vast amounts of log data, allowing your team to focus on resolving issues faster.

This feature is a powerful demonstration of how `CSharpEssentials.LoggerHelper.AI` can transform raw log data into actionable insights, improving diagnostic speed and overall operational efficiency.
![Dashboard AI RAG Example](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper.AI/Docs/dashboard_AI_Rag_Example.png)

---

### 🕵️ AI-Powered Root Cause Analysis with CorrelateTrace

Beyond simple log retrieval, the **AI Assistant** can perform powerful root cause analysis by correlating logs and distributed traces. This is particularly useful for debugging complex issues in microservice architectures without having to manually sift through data.

#### **Use Case: Diagnosing a Timeout Error in a Chain of Responsibility Pattern**

Imagine you're troubleshooting a slow request or a timeout that occurs within a complex interaction, such as a Chain of Responsibility pattern where multiple HTTP calls are made. Instead of manually searching logs and traces across multiple services, you can let the AI Assistant do the heavy lifting.

**Real-world Scenario:** In a realistic application, a single user request might trigger a cascade of internal HTTP calls, each potentially handled by a different microservice. If one of these services introduces a delay, it can lead to a downstream timeout for the original request. Manually pinpointing the exact service or component causing the slowdown can be time-consuming and tedious.

**Simulating the Scenario for Testing:**
You can easily simulate this scenario using the `CSharpEssentials.LoggerHelper.Telemetry` package.
1.  **Trigger a Simulated Latency**: Using the demo project (which leverages `CSharpEssentials.HttpHelper` for mocking `HttpClient` behavior), you can invoke an HTTP endpoint that intentionally introduces a delay in one of its internal spans. This simulates a slow external dependency or a bottleneck in your application's chain of responsibility.
    For example, make a `GET` request to `http://localhost:1234/Telemetry/Simple` with a `SecondsDelay` parameter set to `40`. This will simulate a 40-second delay in one of the internal HTTP calls.

![scalar demo delay](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper.AI/Docs/scalar_http_Simple_delay.png)    
    
2.  **Capture the `IdTransaction`**: Upon receiving the (potentially timed-out) response, extract the `IdTransaction` (which corresponds to the `TraceId` of the activity). This ID links all the telemetry data for that specific request.
 
![scalar demo delay](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper.AI/Docs/scalar_http_Simple_delay_response.png)    
    
3.  **LLM Diagnosis via the Dashboard**:
    Now, with the `TraceId` in hand, navigate to the `CSharpEssentials.LoggerHelper.Dashboard` and use the AI Assistant. Provide a natural language query like: **"I have an issue with a slow request. Can you find the suspicious trace and tell me the root cause of the timeout?"**
    
    *   **Action**: Select `CorrelateTrace`.
    *   **Trace ID**: Input the `IdTransaction` you captured (e.g., `bf90d68e05126496e2ae2b5c45d3c4cd`).
    *   **System Prompt**: Use a specialized prompt such as: "You are an SRE assistant specialized in distributed tracing. Analyze the provided traces and logs, identify the longest-running span, and explain why the operation timed out. If you find a specific error, mention the service and the error message."

    The LLM, acting as a specialized Site Reliability Engineer (SRE), analyzes the correlated data provided by the `CorrelateTrace` action. It identifies the longest-running span within the trace and pinpoints the exact service or operation that caused the delay.

    **Example Output from the Dashboard:**
![Dashboard AI RAG Example](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper.AI/Docs/dashboard_AI_CorrelateTrace_Example.png)
   

4.  **Actionable Insights**:
    The LLM's response provides a clear diagnosis, explaining **why** the issue occurred and where to look. This transforms raw telemetry into an actionable summary, allowing developers and SREs to dramatically reduce their mean time to resolution (MTTR) by avoiding tedious manual database searches.

This feature showcases how `CSharpEssentials.LoggerHelper.AI` transforms raw data into intelligent, actionable insights. Here's an example of how you can test this functionality via cURL:

```curl
curl http://localhost:1234/AI/run \
  --request POST \
  --header 'Content-Type: application/json' \
  --data '{
  "docId": null,
  "traceId": "bf90d68e05126496e2ae2b5c45d3c4cd", # Replace with your actual TraceId from the simulated call
  "query": "I have an issue with a slow request. Can you find the suspicious trace and tell me the root cause of the timeout?",
  "system": "You are an SRE assistant specialized in distributed tracing. Analyze the provided traces and logs, identify the longest-running span, and explain why the operation timed out. If you find a specific error, mention the service and the error message.",
  "action": "CorrelateTrace",
  "fileName": "getTraces.sql",
  "dtStart": "2022-09-22T08:00:00",
  "topResultsOnQuery": 100
}'


---

### 🚨 AI-Powered Anomaly Detection

The **AI Assistant** excels at proactively identifying anomalies within your operational metrics, transforming raw data into actionable alerts and insights. This feature helps SREs and developers quickly pinpoint unusual behavior, understand its root cause, and implement timely solutions, significantly reducing the mean time to detect (MTTD) and mean time to resolve (MTTR) critical issues.

#### **Use Case: Detecting Abnormal Error Rates or Latency**

Imagine your application experiences a sudden spike in error rates or an unexpected increase in request latency. Manually sifting through dashboards and logs to find the anomaly and its cause can be a time-consuming process. The `DetectAnomaly` action automates this by leveraging the power of LLMs.

**Scenario:** A critical microservice starts exhibiting higher-than-usual error rates or response times.

**How to use the `DetectAnomaly` action:**

1.  **Configure your Metric Query**:
    Ensure you have a SQL query file, such as `getMetrics.sql`, within your `DetectAnomaly` folder. This query should retrieve the time-series data for the metric you want to monitor (e.g., `http.client.request.duration`, error counts, CPU usage).

    The corresponding `getMetrics.txt` file defines how the fetched data should be structured for the LLM. For instance:

    ```
    TraceId: {TraceId} | LogEvent: {TraceJson} | Score: {Value}
    ```

    This structure ensures the AI receives key information like the `TraceId`, the full `LogEvent` (potentially JSON data), and a calculated `Score` (e.g., Z-score or anomaly score) for each data point.

2.  **Use the AI Assistant Dashboard**:
    Navigate to the **AI Assistant** page in your dashboard.
    *   **Action**: Select `DetectAnomaly`.
    *   **File Name**: Choose the `getMetrics.sql` file.
    *   **Query**: Enter a natural language question asking the AI to analyze the context for anomalies, for example: "Analyze the data in context. Is there an anomaly? If so, what is the root cause and possible solution?"
    *   **Start Date / End Date**: Define the time window for the metric data you want to analyze.
    *   **Top Records**: Specify the number of records to fetch.
    *   **System Prompt**: Provide specific instructions to guide the AI's analysis and recommendations: "You are a systems analyst with expertise in observability. Analyze the provided metrics and logs (CONTEXT) to identify the root cause of the detected anomaly and recommend mitigation. Prioritize abnormal error rates and latency."

    The system will execute the `getMetrics.sql` query, retrieve the relevant metric data and associated logs, perform statistical analysis (like Z-score calculation), and then use this enriched data as context for the LLM. The AI will then identify any anomalies, explain their potential root causes, and suggest possible solutions.

    **Example Output from the Dashboard:**
![Dashboard AI detect anoamly Example](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper.AI/Docs/dashboard_AI_DetectAnomaly_Example.png)

    