using System.Text.Json;
using System.Text.Json.Nodes;
using Alba;
using Microsoft.Extensions.DependencyInjection;
using PapirFly.Application.Articles;

namespace PapirFly.IntegrationTests;

public sealed class ArticlesTests(ApiFixture api) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    private const string ArticlesUrl = "/api/articles";
    private const string BatchUrl = "/api/articles-concurrent";
    private const string ValidJson = """
        {"name":"Branded Memory Stick","description":"Branded 16 GB memory stick",
         "category":"USB flash drive","price":17.89,"currency":"NOK"}
        """;

    public Task InitializeAsync() => api.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_returns_the_contract_and_get_retrieves_the_same_article()
    {
        var created = await Create();
        Assert.True(created.GetProperty("article_id").GetInt32() > 0);
        Assert.NotEqual(Guid.Empty, created.GetProperty("version").GetGuid());
        Assert.Equal("Branded Memory Stick", created.GetProperty("name").GetString());
        Assert.Equal("Branded 16 GB memory stick", created.GetProperty("description").GetString());
        Assert.Equal("USB flash drive", created.GetProperty("category").GetString());
        Assert.Equal(17.89m, created.GetProperty("price").GetDecimal());
        Assert.Equal("NOK", created.GetProperty("currency").GetString());
        Assert.Equal(7, created.EnumerateObject().Count());

        var loaded = await api.Send("GET", Url(created));
        Assert.Equal(created.GetRawText(), loaded.Json.GetRawText());
    }

    [Fact]
    public async Task Create_accepts_free_articles_and_omits_missing_optional_fields()
    {
        var result = await Create("""{"name":"Mug","description":"Porcelain mug","price":0}""");
        Assert.Equal(0, result.GetProperty("price").GetDecimal());
        Assert.False(result.TryGetProperty("currency", out _));
        Assert.False(result.TryGetProperty("category", out _));
    }

    [Fact]
    public async Task Create_accepts_exact_length_limits_and_decimal_precision()
    {
        var input = JsonNode.Parse(ValidJson)!.AsObject();
        input["name"] = new string('n', 64);
        input["description"] = new string('d', 2048);
        input["category"] = new string('c', 64);
        input["price"] = 123.456789m;
        var created = await Create(input.ToJsonString());
        Assert.Equal(123.456789m, created.GetProperty("price").GetDecimal());
        Assert.Equal(2048, created.GetProperty("description").GetString()!.Length);
    }

    public static IEnumerable<object[]> InvalidArticles()
    {
        foreach (var field in new[] { "name", "description", "price", "currency" })
        {
            var input = JsonNode.Parse(ValidJson)!.AsObject();
            input.Remove(field);
            yield return [input.ToJsonString()];
        }

        (string Field, object? Value)[] invalidValues =
        [
            ("name", null), ("name", " "), ("name", new string('n', 65)),
            ("description", ""), ("description", new string('d', 2049)),
            ("category", new string('c', 65)), ("price", null), ("price", -1),
            ("price", "17.89"), ("price", true), ("currency", ""), ("currency", "ZZZ"),
            ("currency", "nok"), ("article_id", 42), ("article_id", null),
            ("version", Guid.NewGuid()), ("unexpected", "value")
        ];
        foreach (var (field, value) in invalidValues)
        {
            var input = JsonNode.Parse(ValidJson)!.AsObject();
            input[field] = JsonSerializer.SerializeToNode(value);
            yield return [input.ToJsonString()];
        }
        foreach (var invalid in new[] { "null", "[]", "{}", "{", "" })
            yield return [invalid];
    }

    [Theory]
    [MemberData(nameof(InvalidArticles))]
    public async Task Create_rejects_invalid_input_without_writing(string json)
    {
        var error = await api.Send("POST", ArticlesUrl, json, 400);
        Assert.Equal(400, error.Json.GetProperty("status").GetInt32());
        Assert.Empty((await api.Send("GET", ArticlesUrl)).Json.EnumerateArray());
    }

    [Theory]
    [InlineData("999")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-an-id")]
    public async Task Get_missing_or_invalid_id_returns_not_found(string id)
    {
        await api.Send("GET", $"{ArticlesUrl}/{id}", expectedStatus: 404);
    }

    [Theory]
    [InlineData("", 4)]
    [InlineData("?name=bRaNdEd", 3)]
    [InlineData("?name=memory", 2)]
    [InlineData("?category=USB%20flash%20drive", 2)]
    [InlineData("?category=USB", 0)]
    [InlineData("?category=usb%20flash%20drive", 0)]
    [InlineData("?name=branded&category=Mug", 1)]
    [InlineData("?name=memory&category=Mug", 0)]
    [InlineData("?name=256%20GB", 0)]
    public async Task Search_applies_the_documented_filters(string query, int expectedCount)
    {
        await SeedSearchArticles();
        var response = (await api.Send("GET", ArticlesUrl + query)).Json;
        var articles = response.EnumerateArray().ToArray();
        Assert.Equal(expectedCount, articles.Length);
        var ids = articles.Select(a => a.GetProperty("article_id").GetInt32()).ToArray();
        Assert.Equal(ids.Order(), ids);
        if (expectedCount == 1)
            Assert.Equal("Branded Drinking Mug", articles[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Search_returns_an_empty_array_for_an_empty_store()
    {
        Assert.Equal("[]", (await api.Send("GET", ArticlesUrl)).Json.GetRawText());
    }

    [Fact]
    public async Task Search_decodes_url_encoded_unicode_and_reserved_characters()
    {
        await Create(JsonSerializer.Serialize(new
        {
            name = "Čaj & káva",
            description = "Hrnek",
            category = "Hrnky + dárky",
            price = 0
        }));
        var query = $"?name={Uri.EscapeDataString("ČAJ & KÁVA")}&category={Uri.EscapeDataString("Hrnky + dárky")}";
        Assert.Single((await api.Send("GET", ArticlesUrl + query)).Json.EnumerateArray());
    }

    [Fact]
    public async Task Batch_preserves_input_order_and_persists_generated_ids()
    {
        var input = Enumerable.Range(0, 40).Select(i => new { name = $"Mug {i}", description = "Mug", price = 0 });
        var response = await api.Send("POST", BatchUrl, JsonSerializer.Serialize(input));
        var articles = response.Json.EnumerateArray().ToArray();
        Assert.Equal(40, articles.Length);
        Assert.Equal(input.Select(a => a.name), articles.Select(a => a.GetProperty("name").GetString()));
        Assert.Equal(40, articles.Select(a => a.GetProperty("article_id").GetInt32()).Distinct().Count());
        Assert.All(articles, a => Assert.True(a.GetProperty("article_id").GetInt32() > 0));
        var stored = (await api.Send("GET", ArticlesUrl)).Json.EnumerateArray().ToArray();
        Assert.Equal(40, stored.Length);
        Assert.Equal(articles.OrderBy(a => a.GetProperty("article_id").GetInt32()).Select(a => a.GetRawText()),
            stored.Select(a => a.GetRawText()));
    }

    [Fact]
    public async Task Batch_validates_all_items_before_writing_and_reports_indexed_errors()
    {
        var error = await api.Send("POST", BatchUrl, $"[{ValidJson},{{\"price\":-1}},null]", 400);
        var errors = error.Json.GetProperty("errors");
        Assert.True(errors.TryGetProperty("[1].name", out _));
        Assert.True(errors.TryGetProperty("[1].price", out _));
        Assert.True(errors.TryGetProperty("[2].article", out _));
        Assert.Empty((await api.Send("GET", ArticlesUrl)).Json.EnumerateArray());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[null]")]
    [InlineData("[{\"name\":\"Mug\",\"description\":\"Mug\",\"price\":\"0\"}]")]
    [InlineData("[{\"name\":\"Mug\",\"description\":\"Mug\",\"price\":0,\"article_id\":1}]")]
    public async Task Batch_rejects_invalid_payloads(string json)
    {
        await api.Send("POST", BatchUrl, json, 400);
        Assert.Empty((await api.Send("GET", ArticlesUrl)).Json.EnumerateArray());
    }

    [Fact]
    public async Task Empty_batch_returns_an_empty_array()
    {
        Assert.Equal("[]", (await api.Send("POST", BatchUrl, "[]")).Json.GetRawText());
    }

    [Fact]
    public async Task Parallel_single_and_batch_requests_generate_unique_ids_without_lost_writes()
    {
        var requests = Enumerable.Range(0, 20).Select(_ => api.Send("POST", ArticlesUrl, ValidJson))
            .Concat(Enumerable.Range(0, 5).Select(_ => api.Send("POST", BatchUrl, $"[{ValidJson},{ValidJson}]")));
        await Task.WhenAll(requests);
        var articles = (await api.Send("GET", ArticlesUrl)).Json.EnumerateArray().ToArray();
        Assert.Equal(30, articles.Length);
        Assert.Equal(30, articles.Select(a => a.GetProperty("article_id").GetInt32()).Distinct().Count());
    }

    [Fact]
    public async Task Update_replaces_fields_clears_omitted_optionals_and_rotates_version()
    {
        var created = await Create();
        var input = new JsonObject
        {
            ["name"] = "Updated mug",
            ["description"] = "New description",
            ["price"] = 0,
            ["version"] = created.GetProperty("version").GetString()
        };
        var updated = (await api.Send("PUT", Url(created), input.ToJsonString())).Json;
        Assert.Equal(created.GetProperty("article_id").GetInt32(), updated.GetProperty("article_id").GetInt32());
        Assert.Equal("Updated mug", updated.GetProperty("name").GetString());
        Assert.Equal("New description", updated.GetProperty("description").GetString());
        Assert.Equal(0, updated.GetProperty("price").GetDecimal());
        Assert.False(updated.TryGetProperty("category", out _));
        Assert.False(updated.TryGetProperty("currency", out _));
        Assert.NotEqual(created.GetProperty("version").GetGuid(), updated.GetProperty("version").GetGuid());
        Assert.Equal(updated.GetRawText(), (await api.Send("GET", Url(created))).Json.GetRawText());
    }

    [Fact]
    public async Task Update_with_stale_version_returns_conflict_and_keeps_the_winning_data()
    {
        var created = await Create();
        var first = UpdatePayload(created, "Winner");
        var winner = await api.Send("PUT", Url(created), first);
        await api.Send("PUT", Url(created), UpdatePayload(created, "Loser"), 409);
        Assert.Equal(winner.Json.GetRawText(), (await api.Send("GET", Url(created))).Json.GetRawText());
        // The new version is usable for a subsequent update.
        await api.Send("PUT", Url(created), UpdatePayload(winner.Json, "Next update"));
    }

    [Fact]
    public async Task Two_writers_using_the_same_version_have_exactly_one_winner()
    {
        var created = await Create();
        var results = await Task.WhenAll(
            api.Send("PUT", Url(created), UpdatePayload(created, "Writer A"), expectedStatus: null),
            api.Send("PUT", Url(created), UpdatePayload(created, "Writer B"), expectedStatus: null));
        Assert.Equal(new[] { 200, 409 }, results.Select(r => r.Status).Order());
        var winner = results.Single(r => r.Status == 200);
        Assert.Equal(winner.Json.GetRawText(), (await api.Send("GET", Url(created))).Json.GetRawText());
    }

    [Fact]
    public async Task Ef_concurrency_token_rejects_a_stale_detached_snapshot_at_save_time()
    {
        var created = await Create();
        using var scope = api.Host.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var id = created.GetProperty("article_id").GetInt32();
        var first = (await repository.GetAsync(id, default))!;
        var second = (await repository.GetAsync(id, default))!;
        var version = first.Version;
        first.Name = "Winner";
        second.Name = "Loser";
        await repository.UpdateAsync(first, version, default);
        await Assert.ThrowsAsync<ArticleConflictException>(() => repository.UpdateAsync(second, version, default));
        Assert.Equal("Winner", (await api.Send("GET", Url(created))).Json.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Update_missing_article_returns_not_found()
    {
        var input = JsonNode.Parse(ValidJson)!.AsObject();
        input["version"] = Guid.NewGuid();
        await api.Send("PUT", ArticlesUrl + "/999", input.ToJsonString(), 404);
    }

    [Theory]
    [InlineData("missing-version")]
    [InlineData("empty-version")]
    [InlineData("invalid-version")]
    [InlineData("negative-price")]
    [InlineData("string-price")]
    [InlineData("missing-name")]
    [InlineData("client-id")]
    public async Task Invalid_update_returns_bad_request_and_leaves_the_article_unchanged(string testCase)
    {
        var created = await Create();
        var input = JsonNode.Parse(UpdatePayload(created, "Invalid update"))!.AsObject();
        switch (testCase)
        {
            case "missing-version": input.Remove("version"); break;
            case "empty-version": input["version"] = Guid.Empty; break;
            case "invalid-version": input["version"] = "not-a-uuid"; break;
            case "negative-price": input["price"] = -1; break;
            case "string-price": input["price"] = "17.89"; break;
            case "missing-name": input.Remove("name"); break;
            case "client-id": input["article_id"] = 5; break;
        }
        await api.Send("PUT", Url(created), input.ToJsonString(), 400);
        Assert.Equal(created.GetRawText(), (await api.Send("GET", Url(created))).Json.GetRawText());
    }

    [Fact]
    public async Task Application_hosts_have_isolated_in_memory_stores()
    {
        await Create();
        await using var otherHost = await AlbaHost.For<Program>();
        await otherHost.Scenario(s =>
        {
            s.Get.Url(ArticlesUrl);
            s.StatusCodeShouldBeOk();
            s.ContentShouldBe("[]");
        });
        Assert.Single((await api.Send("GET", ArticlesUrl)).Json.EnumerateArray());
    }

    private async Task<JsonElement> Create(string json = ValidJson) => (await api.Send("POST", ArticlesUrl, json)).Json;
    private static string Url(JsonElement article) => $"{ArticlesUrl}/{article.GetProperty("article_id").GetInt32()}";

    private static string UpdatePayload(JsonElement article, string name)
    {
        var input = JsonNode.Parse(ValidJson)!.AsObject();
        input["name"] = name;
        input["version"] = article.GetProperty("version").GetString();
        return input.ToJsonString();
    }

    private async Task SeedSearchArticles()
    {
        var articles = new[]
        {
            new { name = "Branded Memory Stick", category = "USB flash drive", description = "Stick", price = 0 },
            new { name = "Branded Drinking Mug", category = "Mug", description = "Mug", price = 0 },
            new { name = "Plain Memory Stick", category = "USB flash drive", description = "Stick", price = 0 },
            new { name = "Branded Travel Mug", category = "Travel Mug", description = "Mug", price = 0 }
        };
        await api.Send("POST", BatchUrl, JsonSerializer.Serialize(articles));
    }
}
