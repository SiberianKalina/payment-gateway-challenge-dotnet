using PaymentGateway.Api.Clients.BankingClient.Contracts;

using Refit;

namespace PaymentGateway.Api.Clients.BankingClient;

/// <summary>
/// <para> Refit implementation of the Banking Simulator Endpoints.</para>
/// See <see href="https://github.com/cko-recruitment/#calling-the-simulator"> here</see> for additional info.
/// </summary>
public interface IBankingClient
{
    [Post("/payments")]
    Task<BankPaymentResponse> PostPaymentAsync([Body] BankPaymentRequest request);
}