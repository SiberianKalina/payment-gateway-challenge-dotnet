using Microsoft.AspNetCore.Authentication;

namespace PaymentGateway.Api.Authentication;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderName = "X-API-Key";
}