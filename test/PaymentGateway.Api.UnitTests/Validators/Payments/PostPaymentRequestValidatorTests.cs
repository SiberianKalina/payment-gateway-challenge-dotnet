using FluentAssertions;

using FluentValidation.TestHelper;

using Moq;

using PaymentGateway.Api.Contracts.V1.Payments.Requests;
using PaymentGateway.Api.Contracts.V1.Payments.Requests.Validators;
using PaymentGateway.Api.Infrastructure;

namespace PaymentGateway.Api.UnitTests.Validators.Payments;

public class PostPaymentRequestValidatorTests
{
    private readonly PostPaymentRequestValidator _sut;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider = new();

    public PostPaymentRequestValidatorTests()
    {
        _mockDateTimeProvider
            .Setup(x => x.Now)
            .Returns(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));

        _sut = new PostPaymentRequestValidator(_mockDateTimeProvider.Object);
    }

    [Fact]
    public async Task WhenValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "4532015112830366",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #region CardNumber Tests

    [Theory]
    [InlineData("", "'Card Number' is required")]
    [InlineData(null, "'Card Number' is required")]
    public async Task WhenCardNumberIsEmpty_ShouldHaveValidationError(string? cardNumber, string expectedMessage)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { CardNumber = cardNumber! };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardNumber)
            .WithErrorMessage(expectedMessage);
    }

    [Theory]
    [InlineData("123", "'Card Number' must be within 14-19 character range")]
    [InlineData("1234567890123", "'Card Number' must be within 14-19 character range")]
    [InlineData("12345678901234567890", "'Card Number' must be within 14-19 character range")]
    public async Task WhenCardNumberLengthIsInvalid_ShouldHaveValidationError(string cardNumber, string expectedMessage)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { CardNumber = cardNumber };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardNumber)
            .WithErrorMessage(expectedMessage);
    }

    [Theory]
    [InlineData("1234a23412341234", "'Card Number' must contain only digits, spaces, or hyphens.")]
    [InlineData("....asd1234567890", "'Card Number' must contain only digits, spaces, or hyphens.")]
    [InlineData("abcd123456789012", "'Card Number' must contain only digits, spaces, or hyphens.")]
    public async Task WhenCardNumberContainsNonDigits_ShouldHaveValidationError(string cardNumber, string expectedMessage)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { CardNumber = cardNumber };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardNumber)
            .WithErrorMessage(expectedMessage);
    }

    [Theory]
    [InlineData("1234567890123456", "'Card Number' provided is not valid")]
    [InlineData("1111111111111111", "'Card Number' provided is not valid")]
    public async Task WhenCardNumberFailsLuhnCheck_ShouldHaveValidationError(string cardNumber, string expectedMessage)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { CardNumber = cardNumber };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardNumber)
            .WithErrorMessage(expectedMessage);
    }

    [Theory]
    [InlineData("4532015112830366")] // Visa
    [InlineData("5425233430109903")] // Mastercard
    [InlineData("374245455400126")]  // Amex (15 digits)
    public async Task WhenCardNumberIsValid_ShouldNotHaveValidationError(string cardNumber)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { CardNumber = cardNumber };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CardNumber);
    }

    #endregion

    #region ExpiryMonth Tests

    [Fact]
    public async Task WhenExpiryMonthIsZero_ShouldHaveValidationError()
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { ExpiryMonth = 0 };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth)
            .WithErrorMessage("'Expiry Month' must be between 1 and 12");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(99)]
    public async Task WhenExpiryMonthIsOutOfRange_ShouldHaveValidationError(int month)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { ExpiryMonth = month };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth)
            .WithErrorMessage("'Expiry Month' must be between 1 and 12");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public async Task WhenExpiryMonthIsValid_ShouldNotHaveValidationError(int month)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { ExpiryMonth = month };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiryMonth);
    }

    #endregion

    #region ExpiryYear Tests

    [Fact]
    public async Task WhenExpiryYearIsInPast_ShouldHaveValidationError()
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { ExpiryYear = 2024 };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExpiryYear)
            .WithErrorMessage("'Expiry Year' must be in the future");
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    [InlineData(2030)]
    public async Task WhenExpiryYearIsValid_ShouldNotHaveValidationError(int year)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { ExpiryYear = year };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiryYear);
    }

    #endregion

    #region ExpiryDate Combination Tests

    [Fact]
    public async Task WhenExpiryDateIsInPast_ShouldHaveValidationError()
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { ExpiryMonth = 12, ExpiryYear = 2024 };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor("ExpiryDate")
            .WithErrorMessage("The card expiry date must be in the future");
    }

    [Theory]
    [InlineData(2, 2025)]
    [InlineData(12, 2025)]
    [InlineData(1, 2026)]
    public async Task WhenExpiryDateIsInFuture_ShouldNotHaveValidationError(int month, int year)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { ExpiryMonth = month, ExpiryYear = year };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    #endregion

    #region Currency Tests

    [Theory]
    [InlineData("", "'Currency' is required")]
    [InlineData(null, "'Currency' is required")]
    public async Task WhenCurrencyIsEmpty_ShouldHaveValidationError(string? currency, string expectedMessage)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Currency = currency! };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage(expectedMessage);
    }

    [Theory]
    [InlineData("US", "'Currency' must be 3 characters long")]
    [InlineData("USDD", "'Currency' must be 3 characters long")]
    [InlineData("A", "'Currency' must be 3 characters long")]
    public async Task WhenCurrencyLengthIsInvalid_ShouldHaveValidationError(string currency, string expectedMessage)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Currency = currency };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage(expectedMessage);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("XYZ")]
    [InlineData("JPY")]
    public async Task WhenCurrencyIsNotInAllowedList_ShouldHaveValidationError(string currency)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Currency = currency };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("'Currency' must be one of: USD, EUR, GBP");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public async Task WhenCurrencyIsValid_ShouldNotHaveValidationError(string currency)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Currency = currency };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    #endregion

    #region Amount Tests

    [Fact]
    public async Task WhenAmountIsZero_ShouldHaveValidationError()
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Amount = 0 };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("'Amount' must be greater than 0");
    }

    [Fact]
    public async Task WhenAmountIsNegative_ShouldHaveValidationError()
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Amount = -10 };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("'Amount' must be greater than 0");
    }

    [Theory]
    [InlineData(1)]      // $0.01
    [InlineData(100)]    // $1.00
    [InlineData(1050)]   // $10.50
    [InlineData(999999)] // $9999.99
    public async Task WhenAmountIsValid_ShouldNotHaveValidationError(int amount)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Amount = amount };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    #endregion

    #region Cvv Tests

    [Fact]
    public async Task WhenCvvIsZero_ShouldHaveValidationError()
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Cvv = "0" };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Cvv);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("12345")]
    public async Task WhenCvvLengthIsInvalid_ShouldHaveValidationError(string cvv)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Cvv = cvv };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Cvv)
            .WithErrorMessage("'Cvv' must be 3-4 characters long");
    }

    [Theory]
    [InlineData("123")]   // 3 digits
    [InlineData("999")]   // 3 digits
    [InlineData("1234")]  // 4 digits
    [InlineData("9999")]  // 4 digits
    public async Task WhenCvvIsValid_ShouldNotHaveValidationError(string cvv)
    {
        // Arrange
        var request = CreateValidRequest();
        request = request with { Cvv = cvv };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Cvv);
    }

    #endregion

    private static PostPaymentRequest CreateValidRequest()
    {
        return new PostPaymentRequest
        {
            CardNumber = "4532015112830366",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };
    }
}
