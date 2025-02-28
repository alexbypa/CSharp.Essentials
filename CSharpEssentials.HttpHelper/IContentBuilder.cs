using System.Text;

namespace CSharpEssentials.HttpHelper;
public interface IContentBuilder {
    HttpContent BuildContent(object body);
}

public class JsonContentBuilder : IContentBuilder {
    public HttpContent BuildContent(object body) {
        if (body == null)
            return null;

        return new StringContent(body.ToString(), Encoding.UTF8, "application/json");
    }
}

public class XmlContentBuilder : IContentBuilder {
    public HttpContent BuildContent(object body) {
        if (body == null)
            return null;

        return new StringContent(body.ToString(), Encoding.UTF8, "application/xml");
    }
}

public class FormUrlEncodedContentBuilder : IContentBuilder {
    public HttpContent BuildContent(object body) {
        if (body is IDictionary<string, string> dict) {
            return new FormUrlEncodedContent(dict);
        }
        return null;
    }
}
public class NoBodyContentBuilder : IContentBuilder {
    public HttpContent BuildContent(object body) {
        return null;
    }
}
