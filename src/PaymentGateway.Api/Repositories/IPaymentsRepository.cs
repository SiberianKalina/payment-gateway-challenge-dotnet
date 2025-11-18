using PaymentGateway.Api.Domain.Payments;

namespace PaymentGateway.Api.Repositories;

public interface IPaymentsRepository
{
    void Save(Payment payment, string merchantId);
    Payment? Get(Guid id, string merchantId);
}