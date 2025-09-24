### AI Package Configuration

To enable the AI features in your project, you must add the **`LoggerAIOptions`** section to your `AppSettings.json` file. 
This section contains all the necessary settings to configure the AI model and its connection to the service.

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
-----

### Configuration Error Diagnostics ⚠️

If you encounter configuration issues with your `appSettings.json` file, don't worry. The `CSharpEssentials.LoggerHelper.AI` logger instance is designed to send an **error message** directly to your dashboard.

 ![Dashboard AI Errors](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper.AI/Docs/Dashboard_AI_Error.png)

As shown in the image below, you'll be able to view the error details in the **Monitor-Sink** menu by filtering the `Sink` for "LoggerHelper.AI". This allows you to quickly identify any missing or invalid configuration keys and resolve the problem.

-----

#### Key Settings

  * **`Model`**: Specifies the name of the AI model to be used. This should match a model available on the specified AI inference service (e.g., `gpt-4o-mini`).
  * **`chatghapikey`**: Your GitHub API key, used for authentication with the chat service.
  * **`FolderSqlLoaderContainer`**: Defines the local path where the SQL query files for the AI are stored. The AI assistant uses these queries to interact with your data.
  * **`Temperature`**: Controls the randomness of the AI model's output. Lower values produce more deterministic and focused responses, while higher values lead to more creative and varied outputs. The value should be between 0.0 and 1.0.
  * **`topScore`**: A parameter used for search results. It determines the number of top-ranking results to be considered by the AI model when retrieving information.
  * **`urlLLM`**: The URL endpoint of the Large Language Model (LLM) service. This is the address where API requests are sent.
  * **`headersLLM`**: A collection of HTTP headers required for API requests, such as authentication tokens or content types.
  * **`httpClientName`**: The name of the `HttpClient` instance used for making requests to the LLM service. This is typically configured in the application's startup file.

`httpClientName` specifies the name of the `HttpClient` used for making requests to the Large Language Model (LLM) service. This name corresponds to a named client configured in your application's startup file, typically using the `CSharpEssentials.HttpHelper` NuGet package.

By using a **named `HttpClient`**, you can centralize the configuration for the LLM service, including the base URL and any required headers like those for authentication. This approach offers several benefits:

* **Reusability**: You can reuse the same client configuration across different parts of your application without repeating code.
* **Centralized Control**: All client-specific settings, such as timeouts and request headers, are managed in one place.
* **Testability**: Named clients simplify testing by allowing you to easily mock or substitute the `HttpClient` for unit tests.

The `CSharpEssentials.HttpHelper` package streamlines this process by providing an easy way to define and manage these named clients, ensuring consistency and maintainability in your codebase.

`FolderSqlLoaderContainer` defines the path where the SQL query files for the AI are stored. These files provide the context for the AI's actions, allowing it to perform specific tasks. The system currently supports four distinct modes, each corresponding to a different action.

Each mode is represented by a specific C# class that inherits from `ILogMacroAction` and performs a unique task, often using an embedded SQL file to query data.

#### 1. `RagAnswerQuery`
This mode is designed to answer user questions based on a specific set of data. It uses **Retrieval-Augmented Generation (RAG)** by fetching the most relevant documents from a vector store and using them as context for the LLM.

* **How it works**: The system embeds the user's query into a vector and uses it to retrieve similar documents from the vector store. The SQL query defined in the `sqlQuery` variable is used to query the vector store. The retrieved documents are then used as the `CONTEXT` for a prompt, and the AI generates a precise and concise answer.

#### 2. `CorrelateTrace`
This mode helps to identify the most suspicious trace within a recent set of logs.

* **How it works**: It fetches the most recent 50 traces. It then composes a list of candidate traces, including details like `TraceId`, `duration`, and `anomaly` status. This list is provided to the LLM as a prompt, and the AI is instructed to select the most suspicious one and explain why.

#### 3. `SummarizeIncident`
This action is used to summarize the root cause, impact, and remediation of a specific incident using a `TraceId`.

* **How it works**: The system fetches up to 200 logs associated with the provided `TraceId`. It builds a compact, chronological timeline from these logs, ensuring the content fits within a specified character budget. This timeline is then passed to the LLM, which generates a concise summary of the incident. The prompt includes an example of the desired output to guide the AI's tone and structure.

#### 4. `DetectAnomaly`
This mode is designed to detect anomalies in a time series of metrics.

* **How it works**: It queries a repository for a specific metric (e.g., `http.client.request.duration`) over a defined time period (e.g., the last 30 minutes). It then calculates the statistical **mean** and **standard deviation** of the data points. Finally, it computes the **Z-score** of the last data point and determines if it indicates an anomaly based on a predefined threshold (e.g., `z >= 3`).

---
### How to Test AI Models

You can test the four AI models and their corresponding actions using two different methods: the web [API demo with Scalar](https://github.com/alexbypa/Csharp.Essentials.Extensions/blob/main/Web.Api/MinimalApi/Endpoints/AI/ApiAIHelperDemo.cs) or the [CSharpEssentials.LoggerHelper.Dashboard](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Dashboard) client application.

#### 1\. Web API Demo with Scalar

A web API demo is available to test the AI actions directly. This method allows you to interact with the backend API without using the frontend dashboard. The API documentation can be accessed via the Scalar interface.

To run a test, you must send a `POST` request to the `http://localhost:1234/AI/run` endpoint with a JSON body.

  * **Example for `RagAnswerQuery` Action:**
      * **Endpoint:** `http://localhost:1234/AI/run`
      * **Body:**
        ```json
        {
          "docId": null,
          "traceId": null,
          "query": "I received http response with httpstatus 401",
          "system": "If you don't find anything in the context, reply with 'I'm sorry but I couldn't find this information in the database!'",
          "action": "RagAnswerQuery",
          "fileName": "getTraces.sql",
          "now": "2025-09-22T08:00:00"
        }
        ```
  * **Result:** The `RagAnswerQuery` action will search for relevant information based on the provided `query` and `fileName`. As shown in the example, if the information is not found in the database, the AI will respond with the message defined in the `system` parameter.

-----

#### 2\. LoggerHelper.Dashboard

You can also test the AI models directly from the `LoggerHelper.Dashboard` client application. This method provides a user-friendly interface for interacting with the AI.

  * **Interface:** The interface includes dropdown menus for `Action` and `File Name`, along with text areas for `Query` and `System` prompts.
  * **Workflow:**
    1.  Select the desired **`Action`** from the dropdown menu (e.g., `RagAnswerQuery`).
    2.  Choose the specific SQL file from the **`File Name`** dropdown. This file is located in the `FolderSqlLoaderContainer` and corresponds to the selected action (e.g., `getTraces.sql` for `RagAnswerQuery`).
    3.  Enter your **`Query`** and a custom **`System`** prompt to guide the AI's response.
    4.  Click **`Send to LLM`** to process the request.

-----

#### Important Note on SQL Syntax

The SQL queries used by the AI actions are retrieved from the `FolderSqlLoaderContainer`. It's crucial to note that the syntax of these queries must be compatible with your specific database provider (e.g., **MSSQL** or **PostgreSQL**). Ensure that the queries are written correctly for the database you are using.

-----

### RAG with SQL Query Files 📊

A key feature of the **AI Assistant** is its ability to perform **Retrieval-Augmented Generation (RAG)** using pre-saved SQL queries. This is useful for fetching specific data from your database to provide context for the LLM.

#### **Use Case: Analyzing Recent Logs**

This example demonstrates how to use the RAG system to analyze recent log entries by referencing a saved SQL query file.

1.  **Prepare your SQL Query**:
    Save your query in a `.sql` file within the `RagAnswerQuery` folder. For this example, let's use `getlogs.sql` as shown in the image below. The query uses placeholders `{now}` for the current timestamp and `{n}` for the number of results, which are replaced dynamically at runtime.

    Here's an example of the `getlogs.sql` file content:

    ```sql
    select "Id", "ApplicationName" "App", "TimeStamp" "Ts", "LogEvent" "Message", "IdTransaction" "TraceId"
    from "LogEntry"
    where "TimeStamp" > {now}
    order by "Id" desc
    limit {n}
    ```

      * `{now}`: The starting date from the user input. The query will return logs from this date onward.
      * `{n}`: The limit on the number of results to fetch, which you can specify in the UI.

2.  **Use the AI Assistant Dashboard**:
    Navigate to the **AI Assistant** page in your dashboard.

      * **Action**: Select `RagAnswerQuery`.
      * **File Name**: Choose the `getlogs.sql` file.
      * **Query**: Enter your natural language question, for example: "Were there any HTTP responses with status 401?"
      * **Data di Partenza (Start Date)**: Set the date to filter the query.
      * **System**: Add any specific instructions for the LLM, like "Stick closely to the context. If you don't find anything, reply with 'sorry but I didn't find anything'".

The system will execute the `getlogs.sql` query, retrieve the relevant log entries, and use that data as context to generate a precise answer to your question.

-----


## Flusso di utilizzo ( ROADMAP )
1. Code Review solid prnciples.
4. (Opzionale) Recupera dati da `retrieval_sources`.
5. Costruisce il prompt finale da passare al modello AI.
5. L' invio del prompt al modello AI avviene tramite il package Csharp.Essential.HttpHelper ( da rimuovere su appSettings la chiave "UseMock": true, viene impostata da program.cs a false) che gestisce la comunicazione HTTP con l'API del modello AI.)
6. (Opzionale) Le risposte del modello possono essere inviate via email tramite `LoggerHelper.Sink.Email`.
7. inserire gli agent per la lettura sul DB ( da implementare)
