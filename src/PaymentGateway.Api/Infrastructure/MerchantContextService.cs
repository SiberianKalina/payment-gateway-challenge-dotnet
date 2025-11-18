namespace PaymentGateway.Api.Infrastructure;

public sealed class MerchantContextService : IMerchantContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MerchantContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentMerchantId()
    {
        var merchantId = _httpContextAccessor.HttpContext?.User
            .FindFirst("merchant_id")?.Value;

        if (string.IsNullOrEmpty(merchantId))
        {
            throw new UnauthorizedAccessException("Merchant context not available");
        }

        return merchantId;
    }
}
