namespace CSharpEssentials.LoggerHelper.AI.Domain;
/// <summary>
/// Represents a single chat message in the prompt.  Each message has an associated role
/// (e.g. "system", "user" or "assistant") and the text content.  Defining this record
/// separately makes it easy to pass arbitrary sequences of messages to the LLM, which
/// allows us to include few‑shot examples and context messages alongside the final user
/// prompt.
/// </summary>
public sealed record ChatPromptMessage(string Role, string Content);