namespace PaymentGateway.Api.Configuration;

public sealed record BankingClientConfig
{
    public const string SectionName = "BankingClient";
    public required Uri BaseUri { get; init; }
}