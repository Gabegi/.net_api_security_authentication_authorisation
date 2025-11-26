# Secure API (.NET)

A production-ready ASP.NET Core API demonstrating comprehensive security practices including JWT authentication, API key authorization, rate limiting, input validation, and secure cookie handling.

## Features

### Authentication & Authorization

- **JWT (JSON Web Tokens)**
  - 15-minute access token expiration
  - 7-day refresh token rotation
  - Configurable issuer and audience validation
  - 5-second clock skew tolerance for testing
  - Secure HTTP-only cookie storage for refresh tokens

- **API Key Authentication**
  - X-API-Key header validation
  - Database-backed key management
  - Expiration date tracking
  - Last-used timestamp auditing
  - Active/inactive status flags

- **Role-Based Access Control**
  - Admin, User, and AdminOrUser roles
  - Custom authorization policies (e.g., MustBeOver18)
  - Granular endpoint protection

### Security Controls

- **Password Security**
  - BCrypt hashing (salted)
  - Minimum 8 characters required
  - Uppercase, lowercase, digit, and special character requirements
  - Configurable special character set

- **Rate Limiting**
  - Fixed-window rate limiting per endpoint
  - Configurable per-environment (disabled in dev, enabled in prod)
  - Prevents brute force attacks on authentication endpoints

- **HTTPS/TLS**
  - HTTP to HTTPS redirection (307 Temporary Redirect)
  - HSTS (HTTP Strict Transport Security) header
  - 1-year max-age with subdomain inclusion
  - Browser preload list support

- **Input Validation**
  - FluentValidation framework
  - Server-side validation on all endpoints
  - Prevents injection attacks (SQL, XSS)
  - Proper error responses (400 Bad Request)

- **CORS (Cross-Origin Resource Sharing)**
  - Configurable allowed origins
  - Credentials support for authenticated requests
  - Limited HTTP methods (GET, POST, PUT, DELETE)
  - Restricted headers (Authorization, Content-Type, X-API-Key)

- **Cookie Security**
  - HttpOnly flag (prevents JavaScript access)
  - Secure flag (HTTPS only)
  - SameSite=Strict (CSRF protection)
  - 7-day expiration for refresh tokens

### Data Protection

- **Database Layer**
  - Entity Framework Core with SQLite (dev) and SQL Server (prod)
  - Parameterized queries (prevents SQL injection)
  - Entity constraints and unique indexes
  - Foreign key relationships

- **Logging**
  - Serilog structured logging
  - File and console sinks
  - Development and production log levels
  - Sensitive data protection in logs

## Project Structure

```
SecureApi/
├── API/
│   ├── Endpoints/
│   │   ├── AuthEndpoints.cs          # Registration, login, refresh, logout
│   │   ├── ProductEndpoints.cs       # Product listing and creation
│   │   └── WebhookEndpoints.cs       # Webhook and partner API endpoints
│   ├── Middleware/
│   │   ├── GlobalExceptionHandler.cs # Centralized error handling
│   │   └── ApiKeyMiddleware.cs       # API key validation
│   ├── Filters/
│   │   └── ValidationFilter.cs       # Input validation filter
│   ├── Extensions/
│   │   ├── AuthorizationExtensions.cs
│   │   └── RateLimitingExtensions.cs
│   └── Helpers/
│       └── HttpContextHelper.cs      # Client IP extraction
├── Application/
│   ├── Services/
│   │   ├── IAuthService.cs           # Authentication business logic
│   │   ├── AuthService.cs
│   │   ├── IAuthResultHandler.cs     # Result mapping and cookies
│   │   ├── AuthResultHandler.cs
│   │   ├── ITokenService.cs          # JWT token generation
│   │   ├── TokenService.cs
│   │   ├── IPasswordHasher.cs        # Password hashing
│   │   └── BCryptPasswordHasher.cs
│   ├── DTOs/
│   │   ├── Requests/
│   │   │   ├── RegisterRequest.cs
│   │   │   ├── LoginRequest.cs
│   │   │   ├── RefreshRequest.cs
│   │   │   └── LogoutRequest.cs
│   │   └── Responses/
│   │       └── TokenResponse.cs      # Access + refresh tokens
│   └── Validators/
│       ├── RegisterValidator.cs
│       ├── LoginValidator.cs
│       └── ProductValidator.cs
└── Infrastructure/
    └── Persistence/
        ├── ApplicationDbContext.cs   # EF Core DbContext
        └── Models/
            ├── User.cs
            ├── Product.cs
            ├── RefreshToken.cs
            └── ApiKey.cs
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQLite (included) or SQL Server 2019+

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd SecureApi

# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### Running the Application

```bash
# Development (HTTP on port 5286)
dotnet run

# Production (HTTPS on port 7012)
dotnet run --launch-profile https
```

The API will be available at:
- Development: `http://localhost:5286`
- Production: `https://localhost:7012`

## Configuration

Configuration is managed through `appsettings.json` with environment-specific overrides:

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides (rate limiting disabled, HTTPS redirect disabled)
- `appsettings.Production.json` - Production overrides (SQL Server, hardened security)

### Key Settings

```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-min-32-characters-long!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7,
    "ClockSkewSeconds": 5
  },
  "Security": {
    "PasswordRequirements": {
      "MinimumLength": 8,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigits": true,
      "RequireSpecialCharacters": true
    },
    "RateLimiting": {
      "EnableRateLimiting": true,
      "Endpoints": {
        "/api/auth/register": {"PermitLimit": 10, "WindowSeconds": 60},
        "/api/auth/login": {"PermitLimit": 10, "WindowSeconds": 60}
      }
    }
  }
}
```

## API Endpoints

### Authentication

| Method | Endpoint | Authentication | Description |
|--------|----------|-----------------|-------------|
| POST | `/api/auth/register` | None | Register new user |
| POST | `/api/auth/login` | None | Login with email/password |
| POST | `/api/auth/refresh` | None | Refresh access token |
| POST | `/api/auth/logout` | JWT | Logout and revoke token |
| GET | `/api/auth/profile` | JWT | Get authenticated user profile |

### Products

| Method | Endpoint | Authentication | Description |
|--------|----------|-----------------|-------------|
| GET | `/api/products` | None | List all products |
| POST | `/api/products` | JWT (Admin) | Create product |

### Webhooks & Partners

| Method | Endpoint | Authentication | Description |
|--------|----------|-----------------|-------------|
| POST | `/api/webhooks/stripe` | API Key | Stripe webhook handler |
| POST | `/api/webhooks/generic` | API Key | Generic webhook handler |
| GET | `/api/partner/status` | API Key | Partner API status |
| GET | `/api/partner/products` | API Key | Partner product listing |

## Security Testing

The application implements comprehensive security controls. Key test scenarios:

- **Authentication**: Valid/invalid tokens, expired tokens, missing auth headers
- **Authorization**: Role-based access, age validation policies
- **Input Validation**: Weak passwords, malformed requests, injection attempts
- **Rate Limiting**: Multiple rapid requests to auth endpoints
- **HTTPS**: HTTP redirects to HTTPS, HSTS headers present
- **API Keys**: Valid/invalid/expired keys on webhook endpoints

## Environment Variables

Override configuration using environment variables (takes precedence over appsettings.json):

```bash
export JWT_SECRET_KEY="your-secret-key"
export JWT_ISSUER="https://your-domain.com"
export JWT_AUDIENCE="https://your-domain.com"
export ASPNETCORE_ENVIRONMENT="Production"
```

## Database

### SQLite (Development)

- Location: `SecureApi/secureapi.db`
- Automatically created on first run
- Includes seeded sample products

### SQL Server (Production)

- Set `DatabaseProvider` to `SqlServer` in appsettings.Production.json
- Update connection string in `SqlServer` connection string
- Run migrations: `dotnet ef database update`

## Middleware Pipeline

The middleware executes in this order:

1. **Global Exception Handler** - Catch and log all errors
2. **HTTPS Redirection** - HTTP → HTTPS
3. **HSTS** - Strict-Transport-Security header
4. **CORS** - Cross-origin request handling
5. **Swagger** - API documentation (dev only)
6. **Rate Limiting** - Request throttling (prod only)
7. **Authentication** - JWT validation
8. **API Key Authentication** - X-API-Key validation
9. **Authorization** - Policy enforcement

## Building & Deployment

### Development Build

```bash
dotnet build
```

### Production Build

```bash
dotnet publish -c Release -o ./publish
```

### Docker (Optional)

Add Dockerfile for containerization if needed.

## Monitoring & Logs

Logs are written to:
- **Console**: Real-time output during development
- **File**: `logs/app-{date}.log`

Log level by environment:
- Development: Debug
- Production: Warning

## Contributing

When modifying security features:
1. Update relevant configuration in appsettings.json
2. Add input validation to DTOs
3. Implement authorization policies for new endpoints
4. Update test scenarios in this README

## License

[Your license here]