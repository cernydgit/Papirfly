using PapirFly.Application.Articles;

namespace PapirFly.UnitTests;

public sealed class ArticleValidatorTests
{
    private static CreateArticleCommand Valid => new() { Name = "Mug", Description = "Porcelain mug", Price = 0 };

    [Fact]
    public void Accepts_exact_length_limits_and_a_free_article_without_currency()
    {
        var input = Valid with { Name = new('n', 64), Description = new('d', 2048), Category = new('c', 64) };
        Assert.Empty(ArticleValidator.Validate(input));
    }

    [Fact]
    public void Reports_all_length_errors_together()
    {
        var input = Valid with { Name = new('n', 65), Description = new('d', 2049), Category = new('c', 65) };
        var errors = ArticleValidator.Validate(input);
        Assert.Equal(new[] { "category", "description", "name" }, errors.Keys.Order());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_missing_or_blank_required_text(string? text)
    {
        var errors = ArticleValidator.Validate(Valid with { Name = text, Description = text });
        Assert.Contains("name", errors.Keys);
        Assert.Contains("description", errors.Keys);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void Price_is_required_and_non_negative(int? price, bool invalid)
    {
        Assert.Equal(invalid, ArticleValidator.Validate(Valid with { Price = price, Currency = "CZK" }).ContainsKey("price"));
    }

    [Theory]
    [InlineData(0, null, true)]
    [InlineData(0, "", true)]
    [InlineData(0, "   ", true)]
    [InlineData(1, null, false)]
    [InlineData(1, "", false)]
    [InlineData(1, "   ", false)]
    [InlineData(1, "CZK", true)]
    [InlineData(1, "EUR", true)]
    [InlineData(1, "NOK", true)]
    [InlineData(1, "XDR", true)]
    [InlineData(1, "ZZZ", false)]
    [InlineData(0, "ZZZ", false)]
    [InlineData(1, "czk", false)]
    public void Validates_currency_conditionally_and_against_iso_codes(int price, string? currency, bool valid)
    {
        Assert.Equal(valid, !ArticleValidator.Validate(Valid with { Price = price, Currency = currency }).ContainsKey("currency"));
    }

    [Fact]
    public void Update_requires_a_nonempty_version()
    {
        var input = new UpdateArticleCommand { Name = "Mug", Description = "Mug", Price = 0 };
        Assert.Contains("version", ArticleValidator.Validate(input).Keys);
        Assert.Contains("version", ArticleValidator.Validate(input with { Version = Guid.Empty }).Keys);
        Assert.Empty(ArticleValidator.Validate(input with { Version = Guid.NewGuid() }));
    }
}
