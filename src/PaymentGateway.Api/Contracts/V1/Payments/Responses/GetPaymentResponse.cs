using PaymentGateway.Api.Domain.Payments;

namespace PaymentGateway.Api.Contracts.V1.Payments.Responses;

public sealed record GetPaymentResponse
{
    public required Guid Id { get; init; }
    public required PaymentStatus Status { get; init; }
    public required string CardNumberLastFour { get; init; }
    public required int ExpiryMonth { get; init; }
    public required int ExpiryYear { get; init; }
    public required string Currency { get; init; }
    public required int Amount { get; init; }
}