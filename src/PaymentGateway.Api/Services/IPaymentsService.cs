using PaymentGateway.Api.Contracts.V1.Payments.Requests;
using PaymentGateway.Api.Domain.Payments;

namespace PaymentGateway.Api.Services;

public interface IPaymentsService
{
    Task<Payment> MakePaymentAsync(PostPaymentRequest request);
    Task<Payment?> GetPaymentAsync(Guid id);
}