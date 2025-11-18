namespace PaymentGateway.Api.Infrastructure;

public interface IDateTimeProvider
{
    DateTimeOffset Now { get; }
}
