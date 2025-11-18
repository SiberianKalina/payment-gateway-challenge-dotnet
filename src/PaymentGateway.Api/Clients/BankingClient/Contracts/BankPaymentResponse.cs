using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Clients.BankingClient.Contracts;

public sealed record BankPaymentResponse
{
    [JsonPropertyName("authorized")]
    public required bool IsAuthorised { get; init; }
    
    [JsonPropertyName("authorization_code")]
    public required string? AuthorisationCode { get; init; }
}