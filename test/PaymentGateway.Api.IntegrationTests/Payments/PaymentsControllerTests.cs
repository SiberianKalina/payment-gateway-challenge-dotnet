using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Contracts.V1.Payments.Requests;
using PaymentGateway.Api.Contracts.V1.Payments.Responses;
using PaymentGateway.Api.Domain.Payments;
using PaymentGateway.Api.Infrastructure;
using PaymentGateway.Api.IntegrationTests.Payments.Fixtures;

namespace PaymentGateway.Api.IntegrationTests.Payments;

/// <summary>
/// Integration tests using the real Mountebank bank simulator.
///
/// Bank Simulator Rules (based on last digit of card number):
/// - Ends with 1, 3, 5, 7, 9 → Authorized (200 OK, authorized: true, random auth code)
/// - Ends with 2, 4, 6, 8 → Declined (200 OK, authorized: false)
/// - Ends with 0 → Service Unavailable (503)
/// - Missing required fields → 400 Bad Request
///
/// Prerequisites: Run `docker-compose up -d` to start the bank simulator on port 8080
/// </summary>
public class PaymentsControllerTests : IClassFixture<PaymentGatewayApiFactory>
{
    private readonly HttpClient _client;
    private readonly IDateTimeProvider _dateTimeProvider;
    private const string PaymentsEndpointRoute = "/api/v1/payments";
    
    private const string ApiKeyHeader = "X-API-Key";
    private const string MerchantOneApiKey = "test-merchant-1";
    private const string MerchantTwoApiKey = "test-merchant-2";
    
    private const string AuthorisedCardNumber = "2222405343248877";
    private const string DeclinedCardNumber = "4242424242424242";
    private const string ServiceUnavailableCardNumber = "2424242424242420";
    private const string InvalidCreditCard = "1234567890123456";

    public PaymentsControllerTests(PaymentGatewayApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(ApiKeyHeader, MerchantOneApiKey);
        _dateTimeProvider = factory.DateTimeProvider;
    }

    #region Happy Path Tests

    [Fact]
    public async Task CreatePayment_WithValidRequest_AndBankApproves_ShouldReturnAuthorizedPayment()
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber);

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);
        var payment = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        payment.Should().BeEquivalentTo(request, options => options.ExcludingMissingMembers());
        payment.Id.Should().NotBeEmpty();
        payment.CardNumberLastFour.Should().Be(GetMaskedCardValue(request.CardNumber));

        // Verify payment can be retrieved
        var getResponse = await _client.GetAsync($"{PaymentsEndpointRoute}/{payment.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedPayment = await getResponse.Content.ReadFromJsonAsync<GetPaymentResponse>();
        retrievedPayment.Should().BeEquivalentTo(payment);
    }

    [Theory]
    [InlineData("EUR")]  
    [InlineData("USD")]
    [InlineData("GBP")]
    public async Task CreatePayment_WithDifferentCurrency_ShouldReturnAuthorized(
        string currency)
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber) with
        {
            Currency = currency
        };
        
        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);
        var payment = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payment!.Status.Should().Be(PaymentStatus.Authorized);
    }

    #endregion

    #region Bank Declines Payment

    [Fact]
    public async Task CreatePayment_WhenBankDeclines_ShouldReturnDeclinedPayment()
    {
        // Arrange
        var request = CreateValidPostRequest(DeclinedCardNumber);

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);
        var payment = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payment!.Status.Should().Be(PaymentStatus.Declined);
        payment.CardNumberLastFour.Should().Be(GetMaskedCardValue(DeclinedCardNumber));
    }
    
    [Fact]
    public async Task CreatePayment_WithDeclinedCards_ShouldReturnDeclined()
    {
        // Arrange
        var request = CreateValidPostRequest(DeclinedCardNumber);

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);
        var payment = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payment!.Status.Should().Be(PaymentStatus.Declined);
    }

    #endregion

    #region Bank Service Unavailable Tests

    [Fact]
    public async Task CreatePayment_WhenBankReturns503_ShouldReturn500()
    {
        // Arrange
        var request = CreateValidPostRequest(ServiceUnavailableCardNumber);

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreatePayment_WithInvalidCardNumber_ShouldReturn400()
    {
        // Arrange
        var request = CreateValidPostRequest(InvalidCreditCard);

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey("CardNumber");
        problemDetails.Errors["CardNumber"].Should().Contain("'Card Number' provided is not valid");
    }

    [Fact]
    public async Task CreatePayment_WithExpiredCard_ShouldReturn400()
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber) with
        {
            ExpiryYear = _dateTimeProvider.Now.AddYears(-1).Year
        };

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails!.Errors.Should().ContainKey("ExpiryDate");
        problemDetails.Errors["ExpiryDate"].First().Should().Contain("must be in the future");
    }

    [Fact]
    public async Task CreatePayment_WithUnsupportedCurrency_ShouldReturn400()
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber) with { Currency = "ZZZ" };

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails!.Errors.Should().ContainKey("Currency");
        problemDetails.Errors["Currency"].First().Should().Contain("must be one of: USD, EUR, GBP");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreatePayment_WithInvalidAmount_ShouldReturn400(int amount)
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber) with { Amount = amount };

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails!.Errors.Should().ContainKey("Amount");
    }

    [Theory]
    [InlineData("12")]      // Too short
    [InlineData("12345")]   // Too long
    public async Task CreatePayment_WithInvalidCvv_ShouldReturn400(string cvv)
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber) with { Cvv = cvv };

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails!.Errors.Should().ContainKey("Cvv");
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task CreatePayment_WithoutApiKey_ShouldReturn401()
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber);
        
        var factory = new PaymentGatewayApiFactory();
        var unauthenticatedClient = factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync(PaymentsEndpointRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePayment_WithInvalidApiKey_ShouldReturn401()
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber);

        var factory = new PaymentGatewayApiFactory();
        var clientWithBadAuth = factory.CreateClient();
        clientWithBadAuth.DefaultRequestHeaders.Add(ApiKeyHeader, "invalid-key");

        // Act
        var response = await clientWithBadAuth.PostAsJsonAsync(PaymentsEndpointRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Payment Retrieval Tests

    [Fact]
    public async Task GetPayment_WhenPaymentExists_ShouldReturn200()
    {
        // Arrange
        var createRequest = CreateValidPostRequest(AuthorisedCardNumber);

        var createResponse = await _client.PostAsJsonAsync(PaymentsEndpointRoute, createRequest);
        var createdPayment = await createResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Act
        var response = await _client.GetAsync($"{PaymentsEndpointRoute}/{createdPayment!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payment = await response.Content.ReadFromJsonAsync<GetPaymentResponse>();
        payment.Should().BeEquivalentTo(createdPayment);
    }

    [Fact]
    public async Task GetPayment_WhenPaymentDoesNotExist_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync($"{PaymentsEndpointRoute}/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPayment_FromDifferentMerchant_ShouldReturn404()
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber);

        var createResponse = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);
        var createdPayment = await createResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Act
        var factory = new PaymentGatewayApiFactory();
        var merchant2Client = factory.CreateClient();
        merchant2Client.DefaultRequestHeaders.Add(ApiKeyHeader, MerchantTwoApiKey);

        var response = await merchant2Client.GetAsync($"{PaymentsEndpointRoute}/{createdPayment!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Card Masking Tests
    
    [Fact]
    public async Task CreatePayment_ShouldMaskCardNumber_ToLast4Digits()
    {
        // Arrange
        var request = CreateValidPostRequest(AuthorisedCardNumber);

        // Act
        var response = await _client.PostAsJsonAsync(PaymentsEndpointRoute, request);
        var payment = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        payment!.CardNumberLastFour.Should().Be(string.Join("", AuthorisedCardNumber.TakeLast(4)));
        payment.CardNumberLastFour.Length.Should().Be(4);
    }

    #endregion

    #region Helper Functions
    private PostPaymentRequest CreateValidPostRequest(string creditCardNumber)
    {
        return new PostPaymentRequest
        {
            CardNumber = creditCardNumber,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 5000,
            Cvv = "789"
        };
    }
    
    private string GetMaskedCardValue(string creditCardNumber)
    {
        return string.Join("", creditCardNumber.TakeLast(4));
    }
    #endregion
}