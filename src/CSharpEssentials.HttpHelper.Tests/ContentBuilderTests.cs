using Xunit.Abstractions;

namespace CSharpEssentials.HttpHelper.Tests;

public class ContentBuilderTests {
    private readonly ITestOutputHelper _output;

    public ContentBuilderTests(ITestOutputHelper output) {
        _output = output;
    }

    public static TheoryData<IContentBuilder, string> Builders => new() {
        { new JsonContentBuilder(), "application/json" },
        { new XmlContentBuilder(), "application/xml" },
        { new StringContentBuilder("text/plain"), "text/plain" },
    };

    [Theory]
    [MemberData(nameof(Builders))]
    public async Task BuildContent_WithBody_ReturnsUtf8StringContentWithMediaType(IContentBuilder builder, string mediaType) {
        _output.WriteLine($"[Scenario] {builder.GetType().Name}.BuildContent con body stringa");
        _output.WriteLine($"[Atteso] StringContent con media type {mediaType}, charset utf-8 e contenuto del body");

        var content = builder.BuildContent("<payload/>");

        _output.WriteLine($"[Restituito] {content?.Headers.ContentType}");
        Assert.IsType<StringContent>(content);
        Assert.Equal(mediaType, content.Headers.ContentType!.MediaType);
        Assert.Equal("utf-8", content.Headers.ContentType.CharSet);
        Assert.Equal("<payload/>", await content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(Builders))]
    public void BuildContent_NullBody_ReturnsNull(IContentBuilder builder, string mediaType) {
        _output.WriteLine($"[Scenario] {builder.GetType().Name} ({mediaType}).BuildContent con body null");
        _output.WriteLine("[Atteso] null");

        var content = builder.BuildContent(null!);

        Assert.Null(content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Ctor_BlankMediaType_Throws(string? mediaType) {
        _output.WriteLine($"[Scenario] new StringContentBuilder('{mediaType}')");
        _output.WriteLine("[Atteso] ArgumentException");

        Assert.ThrowsAny<ArgumentException>(() => new StringContentBuilder(mediaType!));
    }
}
