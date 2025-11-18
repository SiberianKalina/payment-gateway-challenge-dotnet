using System.Net;

namespace PaymentGateway.Api.Exceptions;

public class BaseException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}