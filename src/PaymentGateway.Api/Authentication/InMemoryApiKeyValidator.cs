namespace PaymentGateway.Api.Authentication;

// Quick Authentication Mockup, making the assumption that this would already be available for
// consumption via nuget package or equivalent. Skipping adding test coverage here as assuming
// the business logic for this would live in a different code repository.
public sealed class InMemoryApiKeyValidator : IApiKeyValidator
{
    // In production, this would be stored in a database
    private static readonly Dictionary<string, (string MerchantId, string MerchantName)> ValidApiKeys = new()
    {
        { "test-api-key-merchant-1", ("merchant-1", "Test Merchant 1") },
        { "test-api-key-merchant-2", ("merchant-2", "Test Merchant 2") },
        { "test-api-key-merchant-3", ("merchant-3", "Test Merchant 3") },
        { "test-merchant-1", ("integration-test-merchant-1", "Integration Test Merchant 1") },
        { "test-merchant-2", ("integration-test-merchant-2", "Integration Test Merchant 2") }
    };

    public Task<ApiKeyValidationResult> ValidateAsync(string apiKey)
    {
        if (ValidApiKeys.TryGetValue(apiKey, out var merchantInfo))
        {
            return Task.FromResult(new ApiKeyValidationResult
            {
                IsValid = true,
                MerchantId = merchantInfo.MerchantId,
                MerchantName = merchantInfo.MerchantName
            });
        }

        return Task.FromResult(new ApiKeyValidationResult
        {
            IsValid = false
        });
    }
}