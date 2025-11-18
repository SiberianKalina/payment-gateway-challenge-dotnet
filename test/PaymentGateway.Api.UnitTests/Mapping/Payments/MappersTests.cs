using FluentAssertions;
using PaymentGateway.Api.Clients.BankingClient.Contracts;
using PaymentGateway.Api.Contracts.V1.Payments.Requests;
using PaymentGateway.Api.Domain.Payments;
using PaymentGateway.Api.Mapping.Payments;

namespace PaymentGateway.Api.UnitTests.Mapping.Payments;

public class MappersTests
{
    [Fact]
    public void ToBankRequest_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "4532015112830366",
            ExpiryMonth = 1,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 5000,
            Cvv = "123"
        };

        // Act
        var result = request.ToBankRequest();

        // Assert
        result.Should().BeOfType<BankPaymentRequest>();
        result.CreditCard.Should().Be("4532015112830366");
        result.ExpiryDate.Should().Be("01/2025", "single-digit months should be zero-padded");
        result.Currency.Should().Be("GBP");
        result.Amount.Should().Be(5000);
        result.Cvv.Should().Be("123");
    }

    [Fact]
    public void ToEntity_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "4532015112830366",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 5000,
            Cvv = "123"
        };

        // Act
        var result = request.ToEntity();

        // Assert
        result.Should().BeOfType<Payment>();
        result.CardNumberLastFour.Should().Be("0366", "only last 4 digits should be stored");
        result.ExpiryMonth.Should().Be(12);
        result.ExpiryYear.Should().Be(2025);
        result.Currency.Should().Be("GBP");
        result.Amount.Should().Be(5000);
        result.Status.Should().Be(PaymentStatus.Pending, "initial status should be pending");
        result.AuthorisationCode.Should().BeNull("no authorisation code before bank response");
        result.Id.Should().NotBeEmpty("should generate a unique GUID");
    }

    [Theory]
    [InlineData("4532015112830366", "0366")]
    [InlineData("374245455400126", "0126")]
    [InlineData("1234", "1234")]
    [InlineData("1234567890123456789", "6789")]
    public void ToEntity_ShouldExtractLastFourDigitsForVariousCardLengths(
        string cardNumber, string expectedLastFour)
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 5000,
            Cvv = "123"
        };

        // Act
        var result = request.ToEntity();

        // Assert
        result.CardNumberLastFour.Should().Be(expectedLastFour);
    }

    [Fact]
    public void Mappers_ShouldWorkConsistentlyAndNotModifyOriginalRequest()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "4532015112830366",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 5000,
            Cvv = "123"
        };

        var originalCardNumber = request.CardNumber;

        // Act
        var bankRequest = request.ToBankRequest();
        var entity = request.ToEntity();

        // Assert - Both mappers should map consistently
        bankRequest.Amount.Should().Be(entity.Amount);
        bankRequest.Currency.Should().Be(entity.Currency);
        bankRequest.ExpiryDate.Should().Be($"{entity.ExpiryMonth:D2}/{entity.ExpiryYear}");

        // Assert - Original request should be unchanged
        request.CardNumber.Should().Be(originalCardNumber);
        request.ExpiryMonth.Should().Be(12);
        request.Amount.Should().Be(5000);
    }
}
