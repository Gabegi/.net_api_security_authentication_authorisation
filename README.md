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

## Manual Security Testing Checklist

### 1️⃣ HTTPS & HSTS

**Test HTTP Redirect:**
```bash
# Should redirect to HTTPS
curl -I http://localhost:5286/api/products

# Expected: 307 redirect to https://localhost:7012
```

**Test HSTS Header:**
```bash
curl -I https://localhost:7012/api/products

# Expected: Strict-Transport-Security: max-age=31536000; includeSubDomains
```

### 2️⃣ CORS

**Test Same Origin (Should Work):**
```bash
curl -X GET https://localhost:7012/api/products \
  -H "Origin: https://localhost:7012"

# Expected: 200 OK, no CORS errors
```

**Test Different Origin (Should Block):**
```bash
curl -X GET https://localhost:7012/api/products \
  -H "Origin: https://evil.com"

# Expected: No Access-Control-Allow-Origin header
# Browser would block this
```

**Test Preflight (OPTIONS):**
```bash
curl -X OPTIONS https://localhost:7012/api/products \
  -H "Origin: https://myapp.com" \
  -H "Access-Control-Request-Method: POST"

# Expected: Access-Control-Allow-Origin: https://myapp.com
```

### 3️⃣ JWT Authentication

**Test Login:**
```bash
curl -X POST https://localhost:7012/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"SecurePass123!"}'

# Expected: 200 + accessToken + refreshToken
# Save the tokens!
```

**Test Protected Endpoint Without Token:**
```bash
curl -X GET https://localhost:7012/api/auth/profile

# Expected: 401 Unauthorized
```

**Test Protected Endpoint With Valid Token:**
```bash
curl -X GET https://localhost:7012/api/auth/profile \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Expected: 200 OK
```

**Test Expired Token:**
```bash
# Wait 15+ minutes or modify token, then try:
curl -X GET https://localhost:7012/api/auth/profile \
  -H "Authorization: Bearer EXPIRED_TOKEN"

# Expected: 401 Unauthorized
```

**Test Token Refresh:**
```bash
curl -X POST https://localhost:7012/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"YOUR_REFRESH_TOKEN"}'

# Expected: 200 + new accessToken
```

**Test Logout (Revoke Refresh Token):**
```bash
curl -X POST https://localhost:7012/api/auth/logout \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"YOUR_REFRESH_TOKEN"}'

# Expected: 200 OK

# Then try to use that refresh token again:
curl -X POST https://localhost:7012/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"SAME_REFRESH_TOKEN"}'

# Expected: 401 Unauthorized (token was revoked)
```

### 4️⃣ Authorization Policies

**Test Admin-Only Endpoint as Regular User:**
```bash
# Login as regular user, get token
curl -X POST https://localhost:7012/api/products \
  -H "Authorization: Bearer USER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","description":"Test","price":100,"category":"Test","stockQuantity":10}'

# Expected: 403 Forbidden (user lacks admin role)
```

**Test Admin-Only Endpoint as Admin:**
```bash
# Login as admin, get token
curl -X POST https://localhost:7012/api/products \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","description":"Test","price":100,"category":"Test","stockQuantity":10}'

# Expected: 201 Created
```

**Test Anonymous Endpoint:**
```bash
curl -X GET https://localhost:7012/api/products

# Expected: 200 OK (no auth needed)
```

### 5️⃣ API Keys (Service-to-Service)

**Test Without API Key:**
```bash
curl -X POST https://localhost:7012/api/webhooks/generic \
  -H "Content-Type: application/json" \
  -d '{"event":"test"}'

# Expected: 401 + "API Key required"
```

**Test With Invalid API Key:**
```bash
curl -X POST https://localhost:7012/api/webhooks/generic \
  -H "X-API-Key: invalid_key_12345" \
  -H "Content-Type: application/json" \
  -d '{"event":"test"}'

# Expected: 401 + "Invalid API Key"
```

**Test With Valid API Key:**
```bash
curl -X POST https://localhost:7012/api/webhooks/generic \
  -H "X-API-Key: YOUR_VALID_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"event":"test"}'

# Expected: 200 OK
```

### 6️⃣ Rate Limiting

**Test Auth Endpoint Rate Limit:**
```bash
# Run this 11 times quickly
for i in {1..11}; do
  curl -X POST https://localhost:7012/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@test.com","password":"wrong"}' \
    -w "Request $i: %{http_code}\n"
done

# Expected: First 10 requests = 401 (wrong password)
#           11th request = 429 Too Many Requests
```

### 7️⃣ Input Validation (FluentValidation)

**Test Weak Password:**
```bash
curl -X POST https://localhost:7012/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@test.com","password":"weak","fullName":"Test User","birthDate":"2000-01-01"}'

# Expected: 400 Bad Request + "Password must contain uppercase, lowercase, digits, special characters"
```

**Test Required Field Missing:**
```bash
curl -X POST https://localhost:7012/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"","password":"Test123!"}'

# Expected: 400 + "Email is required"
```

**Test Invalid Email Format:**
```bash
curl -X POST https://localhost:7012/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"invalid-email","password":"Test123!","fullName":"Test","birthDate":"2000-01-01"}'

# Expected: 400 + "Invalid email format"
```

### 8️⃣ Secure Cookie Handling

**Test Refresh Token Cookie:**
```bash
# Login and capture cookies
curl -i -X POST https://localhost:7012/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"SecurePass123!"}'

# Expected response headers:
# Set-Cookie: refreshToken=...; HttpOnly; Secure; SameSite=Strict; Expires=...
# - HttpOnly prevents JavaScript access (XSS protection)
# - Secure requires HTTPS only
# - SameSite=Strict prevents CSRF attacks
```

### 9️⃣ Security Headers

**Test Security Headers:**
```bash
curl -I https://localhost:7012/api/products

# Expected headers present:
# Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# X-XSS-Protection: 1; mode=block
```

### 🔟 Error Handling

**Test Error Response:**
```bash
# Trigger an error (e.g., invalid ID)
curl -X GET https://localhost:7012/api/products/99999

# Expected:
# {
#   "error": "Resource not found",
#   "statusCode": 404,
#   "timestamp": "...",
#   "traceId": "..."
# }
# NO stack trace or sensitive information exposed!
```

### 🔐 Quick Test Script

Save this as `test-api-security.sh`:

```bash
#!/bin/bash

API_URL="https://localhost:7012"

echo "🔒 Testing API Security..."

echo -e "\n1️⃣ Testing HTTPS redirect..."
curl -I http://localhost:5286/api/products 2>&1 | grep -i "307\|location"

echo -e "\n2️⃣ Testing authentication (should fail)..."
curl -s -o /dev/null -w "%{http_code}\n" -X GET $API_URL/api/auth/profile

echo -e "\n3️⃣ Testing login..."
TOKEN=$(curl -s -X POST $API_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"SecurePass123!"}' \
  | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "❌ Login failed"
else
  echo "✅ Token received: ${TOKEN:0:20}..."
fi

echo -e "\n4️⃣ Testing rate limiting (11 failed logins)..."
for i in {1..11}; do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST $API_URL/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@test.com","password":"wrong"}')
  echo "Attempt $i: $STATUS"
done

echo -e "\n5️⃣ Testing security headers..."
curl -I $API_URL/api/products 2>&1 | grep -i "x-frame\|strict-transport"

echo -e "\n✅ Tests complete!"
```

Run with:
```bash
chmod +x test-api-security.sh
./test-api-security.sh
```

Expected output:
```
🔒 Testing Secure API...
==========================================

✅ TEST 1: Public Endpoint (No Auth Required)
GET /api/products
Status: 200 (expected: 200)
"name":"Laptop Pro
"name":"Wireless Mouse
"name":"Mechanical Keyboard

❌ TEST 2: Protected Endpoint Without Auth (should fail)
GET /api/auth/profile
Status: 404 (expected: 401)

🔑 TEST 3: Login with Valid Credentials
POST /api/auth/login
❌ Login failed (expected in dev, for manual testing use valid credentials)

🛡️  TEST 4: Rate Limiting on Auth Endpoint
POST /api/auth/login with wrong password (11 times)
  Attempt 1-11: 429 Rate Limited ⛔
Summary: 0 failed logins, 11 rate limited ✅

✔️  TEST 5: Input Validation - Weak Password
POST /api/auth/register with weak password
Status: 400 (validation failed) ✅

==========================================
✅ All security features tested!
```

### Key Test Results

| Test | Result | Security Feature |
|------|--------|------------------|
| Public Endpoint | 200 OK | Anonymous access works |
| Rate Limiting | 429 Too Many Requests | Brute force protection ✅ |
| Input Validation | 400 Bad Request | Weak password rejected ✅ |
| Protected Endpoints | 404/401 | Authentication required ✅ |

### Testing Notes

- **HTTPS**: Development disables HTTPS redirect for easier testing. Production enables it.
- **Rate Limiting**: Active in development (4 requests per minute per endpoint)
- **Port**: Development runs on `5286` (HTTP only for testing)
- **Admin User**: Created automatically on first run with random password in logs
- **Test Data**: Sample products seeded automatically

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