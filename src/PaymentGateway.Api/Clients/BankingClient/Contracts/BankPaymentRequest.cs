using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Clients.BankingClient.Contracts;

public sealed record BankPaymentRequest
{
    [JsonPropertyName("card_number")]
    public required string CreditCard  { get; init; }
    [JsonPropertyName("expiry_date")]
    public required string ExpiryDate { get; init; }
    [JsonPropertyName("currency")]
    public required string Currency { get; init; }
    [JsonPropertyName("amount")] 
    public required int Amount { get; init; }    
    [JsonPropertyName("cvv")]
    public required string Cvv { get; init; }
}