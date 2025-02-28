using System.Text.Json;
using System.Text;

namespace CSharpEssentials.HttpHelper;
public interface IContentBuilder {
    HttpContent BuildContent(object body);
}

public class JsonContentBuilder : IContentBuilder {
    public HttpContent BuildContent(object body) {
        if (body == null)
            return null;

        var json = JsonSerializer.Serialize(body);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}

//public class XmlContentBuilder : IContentBuilder {
//    public HttpContent BuildContent(object body) {
//        if (body == null)
//            return null;

//        //var xml = XmlHelper.Serialize(body);//TODO: Implementare la classe XmlHelper
//        return new StringContent(json, Encoding.UTF8, "application/xml");
//    }
//}

public class FormUrlEncodedContentBuilder : IContentBuilder {
    public HttpContent BuildContent(object body) {
        if (body is IDictionary<string, string> dict) {
            return new FormUrlEncodedContent(dict);
        }
        // Gestione di eventuali altri formati
        return null;
    }
}
public class NoBodyContentBuilder : IContentBuilder {
    public HttpContent BuildContent(object body) {
        // Nessun content
        return null;
    }
}
