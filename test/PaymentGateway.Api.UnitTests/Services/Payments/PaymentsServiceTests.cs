using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentGateway.Api.Clients.BankingClient;
using PaymentGateway.Api.Clients.BankingClient.Contracts;
using PaymentGateway.Api.Contracts.V1.Payments.Requests;
using PaymentGateway.Api.Domain.Payments;
using PaymentGateway.Api.Infrastructure;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.UnitTests.Services.Payments;

public class PaymentsServiceTests
{
    private readonly Mock<IBankingClient> _bankingClientMock = new();
    private readonly Mock<IPaymentsRepository> _repositoryMock = new();
    private readonly Mock<ILogger<PaymentsService>> _loggerMock = new();
    private readonly Mock<IMerchantContextService> _merchantContextMock = new();
    private readonly PaymentsService _sut;
    private readonly Fixture _fixture = new();

    private const string TestMerchantId = "merchant-123";

    public PaymentsServiceTests()
    {
        _merchantContextMock
            .Setup(x => x.GetCurrentMerchantId())
            .Returns(TestMerchantId);

        _sut = new PaymentsService(
            _loggerMock.Object,
            _bankingClientMock.Object,
            _repositoryMock.Object,
            _merchantContextMock.Object);
    }

    #region MakePaymentRequestAsync Tests

    [Fact]
    public async Task MakePaymentRequestAsync_WhenBankAuthorizes_ShouldReturnAuthorizedPayment()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse
        {
            IsAuthorised = true,
            AuthorisationCode = "auth-code-123"
        };

        _bankingClientMock
            .Setup(x => x.PostPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _sut.MakePaymentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Authorized);
        result.AuthorisationCode.Should().Be(bankResponse.AuthorisationCode);
        result.Amount.Should().Be(request.Amount);
        result.Currency.Should().Be(request.Currency);
        result.CardNumberLastFour.Should().Be("8877");
        result.ExpiryMonth.Should().Be(request.ExpiryMonth);
        result.ExpiryYear.Should().Be(request.ExpiryYear);
    }

    [Fact]
    public async Task MakePaymentRequestAsync_WhenBankDeclines_ShouldReturnDeclinedPayment()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse
        {
            IsAuthorised = false,
            AuthorisationCode = null
        };

        _bankingClientMock
            .Setup(x => x.PostPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _sut.MakePaymentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Declined);
        result.AuthorisationCode.Should().BeNull();
    }

    [Fact]
    public async Task MakePaymentRequestAsync_WhenSuccessful_ShouldCallRepositorySaveWithCorrectPayment()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse
        {
            IsAuthorised = true,
            AuthorisationCode = "auth-code-123"
        };

        _bankingClientMock
            .Setup(x => x.PostPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        Payment? capturedPayment = null;
        string? capturedMerchantId = null;

        _repositoryMock
            .Setup(x => x.Save(It.IsAny<Payment>(), It.IsAny<string>()))
            .Callback<Payment, string>((payment, merchantId) =>
            {
                capturedPayment = payment;
                capturedMerchantId = merchantId;
            });

        // Act
        await _sut.MakePaymentAsync(request);

        // Assert
        _repositoryMock.Verify(
            x => x.Save(It.IsAny<Payment>(), TestMerchantId),
            Times.Once);

        capturedPayment.Should().NotBeNull();
        capturedPayment!.Status.Should().Be(PaymentStatus.Authorized);
        capturedPayment.AuthorisationCode.Should().Be("auth-code-123");
        capturedMerchantId.Should().Be(TestMerchantId);
    }

    [Fact]
    public async Task MakePaymentRequestAsync_WhenBankClientThrowsHttpRequestException_ShouldThrowAndNotSavePayment()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var exception = new HttpRequestException("Bank service unavailable");

        _bankingClientMock
            .Setup(x => x.PostPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ThrowsAsync(exception);

        // Act
        var act = async () => await _sut.MakePaymentAsync(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Bank service unavailable");

        _repositoryMock.Verify(
            x => x.Save(It.IsAny<Payment>(), It.IsAny<string>()),
            Times.Never,
            "Payment should not be saved when bank call fails");
    }

    [Fact]
    public async Task MakePaymentRequestAsync_WhenBankClientThrowsGenericException_ShouldThrowAndNotSavePayment()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var exception = new InvalidOperationException("Unexpected error");

        _bankingClientMock
            .Setup(x => x.PostPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ThrowsAsync(exception);

        // Act
        var act = async () => await _sut.MakePaymentAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unexpected error");

        _repositoryMock.Verify(
            x => x.Save(It.IsAny<Payment>(), It.IsAny<string>()),
            Times.Never,
            "Payment should not be saved when bank call fails");
    }

    [Fact]
    public async Task MakePaymentRequestAsync_WhenSuccessful_ShouldCallBankingClientWithCorrectRequest()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse
        {
            IsAuthorised = true,
            AuthorisationCode = "auth-code-123"
        };

        BankPaymentRequest? capturedBankRequest = null;

        _bankingClientMock
            .Setup(x => x.PostPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .Callback<BankPaymentRequest>(req => capturedBankRequest = req)
            .ReturnsAsync(bankResponse);

        // Act
        await _sut.MakePaymentAsync(request);

        // Assert
        _bankingClientMock.Verify(
            x => x.PostPaymentAsync(It.IsAny<BankPaymentRequest>()),
            Times.Once);

        capturedBankRequest.Should().NotBeNull();
        capturedBankRequest!.CreditCard.Should().Be(request.CardNumber);
        capturedBankRequest.ExpiryDate.Should().Be($"{request.ExpiryMonth:D2}/{request.ExpiryYear}");
        capturedBankRequest.Currency.Should().Be(request.Currency);
        capturedBankRequest.Amount.Should().Be(request.Amount);
        capturedBankRequest.Cvv.Should().Be(request.Cvv);
    }
        
    [Fact]
    public async Task MakePaymentRequestAsync_ShouldGenerateUniquePaymentId()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse
        {
            IsAuthorised = true,
            AuthorisationCode = "auth-code-123"
        };

        _bankingClientMock
            .Setup(x => x.PostPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        // Act
        var result1 = await _sut.MakePaymentAsync(request);
        var result2 = await _sut.MakePaymentAsync(request);

        // Assert
        result1.Id.Should().NotBeEmpty();
        result2.Id.Should().NotBeEmpty();
        result1.Id.Should().NotBe(result2.Id, "Each payment should have a unique ID");
    }

    #endregion

    #region RetrievePaymentByIdAsync Tests

    [Fact]
    public async Task RetrievePaymentByIdAsync_WhenPaymentExists_ShouldReturnPayment()
    {
        // Arrange
        var payment = _fixture.Create<Payment>();

        _repositoryMock
            .Setup(x => x.Get(payment.Id, TestMerchantId))
            .Returns(payment);

        // Act
        var result = await _sut.GetPaymentAsync(payment.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(payment);
    }

    [Fact]
    public async Task RetrievePaymentByIdAsync_WhenPaymentDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.Get(paymentId, TestMerchantId))
            .Returns((Payment?)null);

        // Act
        var result = await _sut.GetPaymentAsync(paymentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RetrievePaymentByIdAsync_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.Get(paymentId, TestMerchantId))
            .Returns((Payment?)null);

        // Act
        await _sut.GetPaymentAsync(paymentId);

        // Assert
        _repositoryMock.Verify(
            x => x.Get(paymentId, TestMerchantId),
            Times.Once);
    }
    #endregion

    #region Helper Methods

    private static PostPaymentRequest CreateValidPaymentRequest()
    {
        return new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 5000,
            Cvv = "789"
        };
    }

    #endregion
}