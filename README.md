# Payment Gateway API

A Payment gateway API built with ASP.NET Core 8.0 that processes card payments through a bank simulator.

## Quick Start

```bash
# Start the bank simulator
docker-compose up -d

# Run the API
dotnet run --project src/PaymentGateway.Api

# Run tests
dotnet test
```

**API Documentation:** Navigate to `https://localhost:7092/swagger` when running locally.
Note that the port can change, but the api should open to the swagger screen on launch if
running via visualstudios/rider.

**Test API Key:** `test-merchant-1`

## Design Considerations

### API Documentation
- Comprehensive XML documentation on all endpoints
- Declarative Swagger attributes (`ProducesResponseType`, `SwaggerDoc`) for complete API specification
- Swagger/OpenAPI integration with request/response examples
- Detailed validation error messages

### Separation of Concerns
- **Controllers** - Thin layer handling HTTP concerns only
- **Validation Layer** - FluentValidation rules separated from controllers via filters
- **Service Layer** - Business logic isolated in `PaymentsService`
- **Repository Pattern** - Data access abstracted behind `IPaymentsRepository`
- **Global Exception Handler** - Basic implementation to centralise exception handling
- **Interface First** - To facilitate testing and DI pattern

**Data Flow:**
```
Request → Validator → Controller → Service → Domain → Response Model
```
- Requests validated before reaching controller
- Controllers map to domain models
- Services contain business logic and orchestration
- Responses mapped from domain back to DTOs

### Testing Strategy
- **Integration Tests** - End-to-end flows using real bank simulator
- **Unit Tests** - Comprehensive coverage of validation, service, and mapping logic
- Using fluentassertions, moq and AutoFixture libraries

### Security & Isolation
- API Key authentication on all endpoints (except health)
- Merchant isolation - each merchant can only access their own payments
- Card number masking - only last 4 digits stored/returned
- full card data never persisted

### Architecture Assumptions
- `GlobalExceptionHandler` and `MerchantContextService` provided via shared NuGet packages
- Authentication middleware provided by platform team
- In-memory repository suitable for demo; production would use persistent storage

## Project Structure
```
src/
├── PaymentGateway.Api/
│   ├── Controllers/V1/          # API endpoints
│   ├── Services/                # Business logic
│   ├── Repositories/            # Data access
│   ├── Mapping/                 # Request/Response transformations
│   ├── Contracts/               # DTOs and validators
│   ├── Clients/BankingClient/   # Bank integration
│   └── Infrastructure/          # Cross-cutting concerns
test/
├── PaymentGateway.Api.UnitTests/
└── PaymentGateway.Api.IntegrationTests/
```

## Key Endpoints

**POST /api/v1/payments** - Process a payment
**GET /api/v1/payments/{id}** - Retrieve payment details
**GET /health** - Health check endpoint

## Future Improvements

**Testing:**
- Separate integration tests for GET/POST operations (currently combined in flow tests for efficiency)
- Performance/load testing for payment processing throughput

**Production Readiness:**
- Circuit breaker pattern for bank client resilience
- Idempotency keys to prevent duplicate payment processing
- Structured logging with correlation IDs across services
- Health check with bank connectivity probe