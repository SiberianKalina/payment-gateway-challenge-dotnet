using PaymentGateway.Api.Domain.Payments;

namespace PaymentGateway.Api.Repositories;

public class PaymentsRepository(ILogger<PaymentsRepository> logger) : IPaymentsRepository
{
    private readonly Dictionary<string, List<Payment>> _paymentsByMerchantId = new();
    
    public void Save(Payment payment, string merchantId)
    {
        logger.LogDebug("Trying to save PaymentId: {PaymentId} for merchantId: {MerchantId}", payment.Id, merchantId);
        if (_paymentsByMerchantId.TryGetValue(merchantId, out var payments))
        {
            payments.Add(payment);
        }
        else
        {
            _paymentsByMerchantId[merchantId] = [payment];
        }
        logger.LogInformation("Successfully Saved PaymentId: {PaymentId} for MerchantId : {MerchantId} to repository"
            , payment.Id
            , merchantId);
    }

    public Payment? Get(Guid id, string merchantId)
    {
        logger.LogDebug("Attempting to retrieve PaymentId: {PaymentId} for MerchantId : {MerchantId}",
            id,
            merchantId);
        var payment = _paymentsByMerchantId
            .GetValueOrDefault(merchantId, [])
            .FirstOrDefault(p => p.Id == id);
        if (payment is null)
        {
            logger.LogDebug("PaymentId: {PaymentId} not found for MerchantId: {MerchantId}",
                id,
                merchantId);
        }
        return payment;
    }
}