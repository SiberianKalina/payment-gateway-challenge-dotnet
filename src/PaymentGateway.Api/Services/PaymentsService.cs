using System.Diagnostics;
using PaymentGateway.Api.Clients.BankingClient;
using PaymentGateway.Api.Contracts.V1.Payments.Requests;
using PaymentGateway.Api.Domain.Payments;
using PaymentGateway.Api.Infrastructure;
using PaymentGateway.Api.Mapping.Payments;
using PaymentGateway.Api.Repositories;

namespace PaymentGateway.Api.Services;

public class PaymentsService(
    ILogger<PaymentsService> logger,
    IBankingClient bankingClient,
    IPaymentsRepository repository,
    IMerchantContextService merchantContextService) : IPaymentsService
{
    public async Task<Payment> MakePaymentAsync(PostPaymentRequest request)
    {
        var merchantId = merchantContextService.GetCurrentMerchantId();
        var paymentId = Guid.NewGuid();
        
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["PaymentId"] = paymentId,
            ["MerchantId"] = merchantId,
            ["Currency"] = request.Currency,
            ["Amount"] = request.Amount
        }))
        {
            logger.LogInformation(
                "Payment request started for merchant {MerchantId}. Amount: {Amount} {Currency}, Card ending: ****{CardLast4}",
                merchantId,
                request.Amount,
                request.Currency,
                new string(request.CardNumber.TakeLast(4).ToArray()));

            try
            {
                var bankPaymentRequest = request.ToBankRequest();

                logger.LogDebug(
                    "Calling banking API for payment {PaymentId}",
                    paymentId);

                // Track banking API performance
                var stopwatch = Stopwatch.StartNew();
                var bankResponse = await bankingClient.PostPaymentAsync(bankPaymentRequest);
                stopwatch.Stop();

                logger.LogInformation(
                    "Banking API responded in {ElapsedMs}ms. Authorized: {IsAuthorized}, AuthCodePresent: {AuthCodePresent}",
                    stopwatch.ElapsedMilliseconds,
                    bankResponse.IsAuthorised,
                    bankResponse.AuthorisationCode is null ? "false" : "true");

                var payment = request.ToEntity() with
                {
                    Id = paymentId,
                    AuthorisationCode = bankResponse.AuthorisationCode,
                    Status = bankResponse.IsAuthorised ? PaymentStatus.Authorized : PaymentStatus.Declined
                };

                repository.Save(payment, merchantId);

                logger.LogInformation(
                    "Payment {PaymentId} completed successfully. Status: {Status}, AuthCodePresent: {AuthCodePresent}",
                    payment.Id,
                    payment.Status,
                    bankResponse.AuthorisationCode is null ? "false" : "true");

                return payment;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex,
                    "Banking API call failed for payment {PaymentId}. Payment will not be processed.",
                    paymentId);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error processing payment {PaymentId}",
                    paymentId);
                throw;
            }
        }
    }

    public Task<Payment?> GetPaymentAsync(Guid id)
    {
        var merchantId = merchantContextService.GetCurrentMerchantId();

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["PaymentId"] = id,
            ["MerchantId"] = merchantId
        }))
        {
            logger.LogDebug(
                "Retrieving payment {PaymentId} for merchant {MerchantId}",
                id,
                merchantId);

            var payment = repository.Get(id, merchantId);

            if (payment == null)
            {
                logger.LogWarning(
                    "Payment {PaymentId} not found for merchant {MerchantId}. Either payment doesn't exist or merchant doesn't have access.",
                    id,
                    merchantId);
            }
            else
            {
                logger.LogDebug(
                    "Payment {PaymentId} retrieved successfully. Status: {Status}",
                    id,
                    payment.Status);
            }

            return Task.FromResult(payment);
        }
    }
}