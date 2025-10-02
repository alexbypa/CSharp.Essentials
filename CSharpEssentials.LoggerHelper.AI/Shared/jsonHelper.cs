using System.Text.Json.Nodes;

namespace CSharpEssentials.LoggerHelper.AI.Shared;
/// <summary>
/// Contiene i metodi per navigare e manipolare un documento JSON utilizzando percorsi dinamici.
/// </summary>
public static class JsonElementPathHelper {
    // Questo metodo è l'equivalente funzionale e compatto di SelectToken(path)
    public static JsonNode? GetNodeByPath(this JsonNode root, string[] pathSteps) {
        JsonNode? currentNode = root;
        Console.WriteLine($"currentNode: {currentNode}");

        foreach (var step in pathSteps) {
            if (currentNode == null)
                return null;

            if (currentNode is JsonArray arrayNode) {
                if (int.TryParse(step, out int index)) {
                    // Naviga l'array (es: "0")
                    currentNode = index >= 0 && index < arrayNode.Count ? arrayNode[index] : null;
                    Console.WriteLine($"currentNode è un array : {currentNode}");
                } else {
                    // Il passo non è un indice per l'array
                    return null;
                }
            } else if (currentNode is JsonObject objectNode) {
                // Naviga l'oggetto (es: "candidates")
                currentNode = objectNode[step];
                Console.WriteLine($"currentNode è un oggetto : {currentNode}");
            } else {
                // Nodo foglia raggiunto prima della fine del percorso
                return null;
            }
        }
        Console.WriteLine($"currentNode alla fine è : {currentNode}");
        return currentNode;
    }
}
