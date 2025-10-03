namespace CSharpEssentials.LoggerHelper.AI.Ports;
public interface IPayloadLLM {
    string buildPayload(string system, string user, string context);
}
public class PayloadGemini : IPayloadLLM {
    public string buildPayload(string system, string user, string context) {
        system_instruction = new SystemInstruction() {
            parts = new List<Part> { new Part { text = system } }
        };
        contents = new List<Content> {
            new Content {
                role = "user",
                parts = new List<Part> { new Part { text = user } }
            },
            new Content {
                role = "model",
                parts = new List<Part> { new Part { text = context } }
            }
        };
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
    public SystemInstruction system_instruction { get; set; } = new SystemInstruction();
    public List<Content> contents { get; set; } = new List<Content>();
    public class Content {
        public string role { get; set; }
        public List<Part> parts { get; set; }
    }
    public class Part {
        public string text { get; set; }
    }
    public class SystemInstruction {
        public List<Part> parts { get; set; }
    }
}


public class PayloadOpenAI : IPayloadLLM {
    public string buildPayload(string system, string user, string context) {
        model = "gpt-4o-mini";
        messages = new List<Message> {
            new Message { role = "system", content = system },
            new Message { role = "user", content = user },
            new Message { role = "assistant", content = context }
        };
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
    public List<Message> messages { get; set; } = new List<Message>();
    public string model { get; set; }
    public double temperature { get; set; }
    public class Message {
        public string role { get; set; }
        public string content { get; set; }
    }
}