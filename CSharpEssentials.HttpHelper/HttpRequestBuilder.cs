using System;
using System.Net.Http;

namespace CSharpEssentials.HttpHelper;
public class HttpRequestBuilder {
    private string _url;
    private HttpMethod _method = HttpMethod.Get; // default
    private IContentBuilder _contentBuilder;
    private object _body;

    public HttpRequestBuilder WithUrl(string url) {
        _url = url;
        return this;
    }

    public HttpRequestBuilder WithMethod(HttpMethod method) {
        _method = method;
        return this;
    }

    public HttpRequestBuilder WithContentBuilder(IContentBuilder contentBuilder) {
        _contentBuilder = contentBuilder;
        return this;
    }

    public HttpRequestBuilder WithBody(object body) {
        _body = body;
        return this;
    }

    public HttpRequestMessage Build() {
        if (string.IsNullOrEmpty(_url))
            throw new InvalidOperationException("Url non impostata.");

        var request = new HttpRequestMessage(_method, _url);

        if (_contentBuilder != null) {
            var content = _contentBuilder.BuildContent(_body);
            if (content != null) {
                request.Content = content;
            }
        }

        return request;
    }
}
