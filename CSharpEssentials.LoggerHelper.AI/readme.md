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


## Flusso di utilizzo ( ROADMAP )
1. Code Review solid prnciples.
4. (Opzionale) Recupera dati da `retrieval_sources`.
5. Costruisce il prompt finale da passare al modello AI.
5. L' invio del prompt al modello AI avviene tramite il package Csharp.Essential.HttpHelper ( da rimuovere su appSettings la chiave "UseMock": true, viene impostata da program.cs a false) che gestisce la comunicazione HTTP con l'API del modello AI.)
6. (Opzionale) Le risposte del modello possono essere inviate via email tramite `LoggerHelper.Sink.Email`.
7. inserire gli agent per la lettura sul DB ( da implementare)
