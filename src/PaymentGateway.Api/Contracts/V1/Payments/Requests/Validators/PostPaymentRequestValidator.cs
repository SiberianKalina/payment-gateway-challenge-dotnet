using FluentValidation;

using PaymentGateway.Api.Infrastructure;

namespace PaymentGateway.Api.Contracts.V1.Payments.Requests.Validators;

public sealed class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    private const string ContainsNonDigitsRegex = @"^\d+$";
    private static readonly HashSet<string> AllowedCurrencies = ["USD", "EUR", "GBP"];
    private readonly IDateTimeProvider _dateTimeProvider;

    public PostPaymentRequestValidator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("'{PropertyName}' is required")
            .Length(14, 19).WithMessage("'{PropertyName}' must be within 14-19 character range")
            .Matches(ContainsNonDigitsRegex)
            .WithMessage("'{PropertyName}' must contain only digits, spaces, or hyphens.")
            .CreditCard().WithMessage("'{PropertyName}' provided is not valid");

        RuleFor(x => x.ExpiryMonth)
            .NotEmpty().WithMessage("'{PropertyName}' is required")
            .InclusiveBetween(1, 12).WithMessage("'{PropertyName}' must be between 1 and 12");

        RuleFor(x => x.ExpiryYear)
            .NotEmpty().WithMessage("'{PropertyName}' is required")
            .GreaterThanOrEqualTo(_dateTimeProvider.Now.Year).WithMessage("'{PropertyName}' must be in the future");

        RuleFor(x => x)
            .Must(x => IsExpiryDateInFuture(x.ExpiryMonth, x.ExpiryYear))
            .WithMessage("The card expiry date must be in the future")
            .WithName("ExpiryDate");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("'{PropertyName}' is required")
            .Length(3).WithMessage("'{PropertyName}' must be 3 characters long")
            .Must(currency => AllowedCurrencies.Contains(currency))
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", AllowedCurrencies)}");

        RuleFor(x => x.Amount)
            .NotEmpty().WithMessage("'{PropertyName}' is required")
            .GreaterThan(0).WithMessage("'{PropertyName}' must be greater than 0");

        RuleFor(x => x.Cvv)
            .NotEmpty().WithMessage("'{PropertyName}' is required")
            .Length(3,4).WithMessage("'{PropertyName}' must be 3-4 characters long")
            .Must(cvv => cvv.ToString().All(char.IsDigit))
            .WithMessage("'{PropertyName}' must contain only numeric characters");
    }

    private bool IsExpiryDateInFuture(int month, int year)
    {
        if (month is < 1 or > 12)
        {
            return false;
        }
        
        var now = _dateTimeProvider.Now;
        var expiryDate = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero)
            .AddMonths(1)
            .AddDays(-1);
        return expiryDate >= now;
    }
}