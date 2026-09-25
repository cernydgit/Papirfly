using Alba;

namespace PapirFly.IntegrationTests;

/// <summary>Verifies the generated OpenAPI contract and the Swagger user interface.</summary>
/// <param name="api">The real application fixture.</param>
public sealed class DocumentationTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    /// <summary>Checks operation descriptions, response schemas and model XML documentation.</summary>
    /// <returns>A task that completes after the OpenAPI assertions.</returns>
    [Fact]
    public async Task Swagger_documents_every_article_operation_and_snake_case_contract()
    {
        var document = (await api.Send("GET", "/swagger/v1/swagger.json")).Json;
        var paths = document.GetProperty("paths");
        (string Path, string Method)[] operations =
        [
            ("/api/articles", "post"), ("/api/articles", "get"),
            ("/api/articles/{articleId}", "get"), ("/api/articles/{articleId}", "put"),
            ("/api/articles-concurrent", "post")
        ];
        foreach (var (path, method) in operations)
        {
            var operation = paths.GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
        }

        var schemas = document.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.GetProperty("ArticleResponse").GetProperty("properties").TryGetProperty("article_id", out _));
        Assert.False(schemas.GetProperty("CreateArticleCommand").GetProperty("properties").TryGetProperty("article_id", out _));
        Assert.True(schemas.GetProperty("UpdateArticleCommand").GetProperty("properties").TryGetProperty("version", out _));
        var name = schemas.GetProperty("CreateArticleCommand").GetProperty("properties").GetProperty("name");
        Assert.Contains("64 characters", name.GetProperty("description").GetString());
        var version = schemas.GetProperty("UpdateArticleCommand").GetProperty("properties").GetProperty("version");
        Assert.Contains("concurrency token", version.GetProperty("description").GetString());
    }

    /// <summary>Checks that the Swagger UI page is served successfully.</summary>
    /// <returns>A task that completes after the page assertions.</returns>
    [Fact]
    public async Task Swagger_ui_is_available()
    {
        await api.Host.Scenario(s =>
        {
            s.Get.Url("/swagger/index.html");
            s.StatusCodeShouldBeOk();
            s.ContentShouldContain("Swagger UI");
        });
    }
}
