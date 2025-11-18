namespace PaymentGateway.Api.Authentication;

public interface IApiKeyValidator
{
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey);
}

public class ApiKeyValidationResult
{
    public bool IsValid { get; init; }
    public string MerchantId { get; init; } = string.Empty;
    public string MerchantName { get; init; } = string.Empty;
}