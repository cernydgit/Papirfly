using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

public static class ArticleValidator
{
    public static Dictionary<string, string[]> Validate(ArticleInput? article)
    {
        var errors = new Dictionary<string, string[]>();
        if (article is null)
        {
            errors["article"] = ["An article is required."];
            return errors;
        }

        CheckText("name", article.Name, Article.NameMaxLength, required: true);
        CheckText("description", article.Description, Article.DescriptionMaxLength, required: true);
        CheckText("category", article.Category, Article.CategoryMaxLength, required: false);

        if (article.Price is null or < 0)
            errors["price"] = ["Price is required and must be a number greater than or equal to zero."];

        if (string.IsNullOrWhiteSpace(article.Currency))
        {
            if (article.Price > 0)
                errors["currency"] = ["Currency is required when price is greater than zero."];
        }
        else if (!CurrencyCodes.Contains(article.Currency))
            errors["currency"] = ["Currency must be an uppercase ISO 4217 code."];

        if (article is UpdateArticleCommand update && (update.Version is null || update.Version == Guid.Empty))
            errors["version"] = ["The version returned when the article was read is required."];

        return errors;

        void CheckText(string field, string? value, int maxLength, bool required)
        {
            if (required && string.IsNullOrWhiteSpace(value))
                errors[field] = ["This field is required and must not be blank."];
            else if (value?.Length > maxLength)
                errors[field] = [$"This field must not exceed {maxLength} characters."];
        }
    }

    public static void EnsureValid(ArticleInput? article)
    {
        var errors = Validate(article);
        if (errors.Count > 0)
            throw new ArticleValidationException(errors);
    }
}
