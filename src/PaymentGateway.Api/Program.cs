using FluentValidation;

using MicroElements.Swashbuckle.FluentValidation.AspNetCore;

using PaymentGateway.Api.Authentication;
using PaymentGateway.Api.Clients.BankingClient;
using PaymentGateway.Api.Configuration;
using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Infrastructure;
using PaymentGateway.Api.Middleware;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services;

using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true);

// Add services to the container.
builder.Services.AddControllers();

// Add HTTP Context Accessor for merchant context
builder.Services.AddHttpContextAccessor();

// Configure Authentication
builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme,
        _ => { });

// Configure Authorization
builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Payment Gateway API",
        Version = "v1",
        Description = "A payment gateway API for processing card payments and retrieving payment history.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Payment Gateway Support"
        }
    });

    // Include XML comments in Swagger documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key authentication. Example: 'X-API-Key: your-api-key'",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = ApiKeyAuthenticationOptions.HeaderName,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Global ExceptionHanlder
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationRulesToSwagger();
builder.Services.AddFluentValidationFilters();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddSingleton<IApiKeyValidator, InMemoryApiKeyValidator>();
builder.Services.AddScoped<IMerchantContextService, MerchantContextService>();
builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddScoped<IPaymentsService, PaymentsService>();

var bankingClientConfig = builder.Configuration
    .GetRequiredSection(BankingClientConfig.SectionName)
    .Get<BankingClientConfig>();
builder.Services.AddRefitClient<IBankingClient>()
    .ConfigureHttpClient(options =>
        options.BaseAddress = bankingClientConfig!.BaseUri
    );

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
