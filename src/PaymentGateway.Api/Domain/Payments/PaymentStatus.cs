using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Domain.Payments;

[JsonConverter(typeof(JsonStringEnumConverter<PaymentStatus>))]
public enum PaymentStatus
{
    Pending,
    Authorized,
    Declined
}