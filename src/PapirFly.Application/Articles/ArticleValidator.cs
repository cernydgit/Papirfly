using PapirFly.Application.Articles.Commands;
using PapirFly.Application.DTOs;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

/// <summary>Applies shared article validation rules to create and update inputs.</summary>
public static class ArticleValidator
{
    /// <summary>Collects all article validation errors without changing the input.</summary>
    /// <param name="article">The input to validate; null is reported as a missing article.</param>
    /// <returns>Errors keyed by contract field name, or an empty dictionary when valid.</returns>
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

    /// <summary>Rejects an input if any shared article validation rule fails.</summary>
    /// <param name="article">The article to validate.</param>
    /// <exception cref="ArticleValidationException">The input contains one or more validation errors.</exception>
    public static void EnsureValid(ArticleInput? article)
    {
        var errors = Validate(article);
        if (errors.Count > 0)
            throw new ArticleValidationException(errors);
    }
}
