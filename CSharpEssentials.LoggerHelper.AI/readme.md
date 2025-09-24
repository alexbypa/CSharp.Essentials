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



  


# Prompt Engineering Package

Questo package ha l’obiettivo di permettere al cliente di personalizzare il comportamento di un modello AI tramite la definizione di prompt strutturati.  
Il cliente potrà inserire direttamente i testi e le viste da utilizzare, senza modificare il codice.

---

## Struttura del Prompt

Il package supporta i seguenti elementi:

1. **System Prompt**  
   - Definisce il comportamento generale dell’AI.  
   - Esempio: *"Sei un assistente che risponde in maniera tecnica e concisa."*  
   - Campo da compilare: `system_prompt`.

2. **User Prompt**  
   - Rappresenta la richiesta diretta dell’utente.  
   - È il contenuto dinamico che arriva durante ogni chiamata al modello.  
   - Campo gestito: `user_prompt`.

3. **Context / Viste**  
   - Blocchi di informazione aggiuntiva a cui l’AI deve fare riferimento.  
   - Possono essere documenti, dati di business, FAQ.  
   - Campo da compilare: `context_views`.

4. **Few-Shot Examples**  
   - Esempi di input e output per guidare stile e comportamento.  
   - Campo opzionale: `few_shot_examples`.

5. **RAG (Retrieval Augmented Generation)** *(opzionale)*  
   - Possibilità di collegare fonti esterne (database, API, knowledge base).  
   - Campo: `retrieval_sources`.

---

## Configurazione

Il cliente compila un file di configurazione (es. `config.yaml`) con i propri testi:

```yaml
system_prompt: >
  Sei un assistente specializzato in [dominio].
  
context_views:
  - "Vista 1: Documento tecnico"
  - "Vista 2: Dati prodotto"
  
few_shot_examples:
  - input: "Domanda esempio"
    output: "Risposta esempio"

---

## Flusso di utilizzo ( ROADMAP )
1. Code Review solid prnciples.
2. Integra gli `user_prompt` dinamici durante la sessione.
3. Se presenti, aggiunge `few_shot_examples`.
4. (Opzionale) Recupera dati da `retrieval_sources`.
5. Costruisce il prompt finale da passare al modello AI.
5. L' invio del prompt al modello AI avviene tramite il package Csharp.Essential.HttpHelper ( da rimuovere su appSettings la chiave "UseMock": true, viene impostata da program.cs a false) che gestisce la comunicazione HTTP con l'API del modello AI.)
6. (Opzionale) Le risposte del modello possono essere inviate via email tramite `LoggerHelper.Sink.Email`.
7. inserire gli agent per la lettura sul DB ( da implementare)
