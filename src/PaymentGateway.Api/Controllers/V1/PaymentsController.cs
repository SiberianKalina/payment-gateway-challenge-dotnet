using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Contracts.V1.Payments.Requests;
using PaymentGateway.Api.Contracts.V1.Payments.Responses;
using PaymentGateway.Api.Filters;
using PaymentGateway.Api.Mapping.Payments;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers.V1;

/// <summary>
/// Handles payment processing operations including creating and retrieving payment transactions.
/// </summary>
/// <remarks>
/// All endpoints require authentication via API key (X-API-Key header).
/// </remarks>
[Route("api/v1/payments")]
[ApiController]
[Authorize]
public class PaymentsController(
    IPaymentsService paymentsService)
    : ControllerBase
{
    /// <summary>
    /// Retrieves a previously processed payment by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the payment to retrieve.</param>
    /// <returns>The payment details if found, otherwise 404 Not Found.</returns>
    /// <response code="200">Returns the payment details including masked card number and transaction status.</response>
    /// <response code="404">The payment was not found or belongs to a different merchant.</response>
    /// <response code="401">Missing or invalid API key.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <remarks>
    /// Sample request:
    ///     GET /api/v1/payments/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     X-API-Key: your-api-key-here
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NotFoundResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetPaymentAsync(Guid id)
    {
        var payment = await paymentsService.GetPaymentAsync(id);

        if (payment is null)
        {
            return NotFound();
        }

        return Ok(payment.ToGetPaymentResponse());
    }

    /// <summary>
    /// Processes a new payment transaction through the banking system.
    /// </summary>
    /// <param name="request">The payment details including card information, amount, and currency.</param>
    /// <returns>The processed payment with status (Authorized or Declined) and masked card details.</returns>
    /// <response code="200">Payment processed successfully. Check the status field to determine if authorized or declined.</response>
    /// <response code="400">Invalid request - validation errors in card details, amount, currency, or expiry date.</response>
    /// <response code="401">Missing or invalid API key.</response>
    /// <response code="500">Payment processing failed due to internal server error.</response>
    /// <response code="503">Service temporarily unavailable.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/payments
    ///     X-API-Key: your-api-key-here
    ///     Content-Type: application/json
    ///
    ///     {
    ///       "cardNumber": "2222405343248877",
    ///       "expiryMonth": 12,
    ///       "expiryYear": 2025,
    ///       "currency": "GBP",
    ///       "amount": 5000,
    ///       "cvv": "123"
    ///     }
    ///
    /// **Important Notes:**
    /// - Amount is in smallest currency unit (e.g., cents/pence): 5000 = $50.00 or £50.00
    /// - Card number must pass Luhn check validation
    /// - Supported currencies: USD, EUR, GBP
    /// - CVV must be 3-4 digits
    /// - Expiry date must be in the future
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ServiceFilter(typeof(ValidationFilter<PostPaymentRequest>))]
    public async Task<ActionResult> PostPaymentAsync(
        [FromBody] PostPaymentRequest request)
    {
        var payment = await paymentsService.MakePaymentAsync(request);
        return Ok(payment.ToPostPaymentResponse());
    }
}