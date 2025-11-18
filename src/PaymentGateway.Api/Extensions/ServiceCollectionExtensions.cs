using FluentValidation;

using PaymentGateway.Api.Filters;

namespace PaymentGateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluentValidationFilters(this IServiceCollection services)
    {
        var validatorTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)))
            .ToList();

        foreach (var validatorType in validatorTypes)
        {
            var modelType = validatorType.BaseType?.GetGenericArguments().FirstOrDefault();
            if (modelType != null)
            {
                var filterType = typeof(ValidationFilter<>).MakeGenericType(modelType);
                services.AddScoped(filterType);
            }
        }

        return services;
    }
}