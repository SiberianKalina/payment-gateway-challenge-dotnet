using FluentValidation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PaymentGateway.Api.Filters;

public class ValidationFilter<T>(IValidator<T> validator, ILogger<ValidationFilter<T>> logger)
    : IAsyncActionFilter
    where T : class
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.ActionArguments.Values.OfType<T>().FirstOrDefault();
        if (request is null)
        {
            await next();
            return;
        }

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            logger.LogWarning(
                "Validation failed for {RequestType}. Errors: {Errors}",
                typeof(T).Name,
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))
            );

            context.Result = new BadRequestObjectResult(
                new ValidationProblemDetails(validationResult.ToDictionary())
            );
            return;
        }

        await next();
    }
}