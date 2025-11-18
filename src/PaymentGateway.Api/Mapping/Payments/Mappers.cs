using PaymentGateway.Api.Clients.BankingClient.Contracts;
using PaymentGateway.Api.Contracts.V1.Payments.Requests;
using PaymentGateway.Api.Contracts.V1.Payments.Responses;
using PaymentGateway.Api.Domain.Payments;

namespace PaymentGateway.Api.Mapping.Payments;

public static class Mappers
{
    public static BankPaymentRequest ToBankRequest(this PostPaymentRequest request)
    {
        return new BankPaymentRequest
        {
            Amount = request.Amount,
            CreditCard = request.CardNumber,
            Currency = request.Currency,
            Cvv = request.Cvv,
            ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}"
        };
    }
    
    public static Payment ToEntity(this PostPaymentRequest request)
    {
        return new Payment
        {
            Amount = request.Amount,
            CardNumberLastFour = string.Join("", request.CardNumber.TakeLast(4)),
            Currency = request.Currency,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Pending,
            AuthorisationCode = null
        };
    }
    
    public static PostPaymentResponse ToPostPaymentResponse(this Payment payment)
    {
        return new PostPaymentResponse
        {
            Id = payment.Id,
            Currency = payment.Currency,
            Amount = payment.Amount,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            CardNumberLastFour = payment.CardNumberLastFour,
            Status = payment.Status
        };
    }
    
    public static GetPaymentResponse ToGetPaymentResponse(this Payment payment)
    {
        return new GetPaymentResponse
        {
            Id = payment.Id,
            Currency = payment.Currency,
            Amount = payment.Amount,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            CardNumberLastFour = payment.CardNumberLastFour,
            Status = payment.Status
        };
    }
}