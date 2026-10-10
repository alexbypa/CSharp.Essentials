using System.Text;

namespace CSharpEssentials.HttpHelper;
public interface IContentBuilder {
    HttpContent BuildContent(object body);
}

/// <summary>
/// Builds a <see cref="StringContent"/> (UTF-8) from <c>body.ToString()</c> with the given media type.
/// </summary>
/// <param name="mediaType">Media type of the content, e.g. "application/json".</param>
public class StringContentBuilder(string mediaType) : IContentBuilder {
    private readonly string _mediaType = !string.IsNullOrWhiteSpace(mediaType)
        ? mediaType
        : throw new ArgumentException("Media type is required.", nameof(mediaType));

    public HttpContent BuildContent(object body) {
        if (body == null)
            return null;

        return new StringContent(body.ToString(), Encoding.UTF8, _mediaType);
    }
}

public class JsonContentBuilder() : StringContentBuilder("application/json") {
}

public class XmlContentBuilder() : StringContentBuilder("application/xml") {
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
