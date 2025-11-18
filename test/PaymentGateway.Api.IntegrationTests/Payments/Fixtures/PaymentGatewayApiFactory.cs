using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaymentGateway.Api.Controllers.V1;
using PaymentGateway.Api.Infrastructure;

namespace PaymentGateway.Api.IntegrationTests.Payments.Fixtures;

public class PaymentGatewayApiFactory : WebApplicationFactory<PaymentsController>
{
    public IDateTimeProvider DateTimeProvider { get; init; }
    
    public PaymentGatewayApiFactory()
    {
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.Setup(x => x.Now)
            .Returns(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
        DateTimeProvider = dateTimeProviderMock.Object;
    }
    
        
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDateTimeProvider));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton(DateTimeProvider);
        });
    }
}
