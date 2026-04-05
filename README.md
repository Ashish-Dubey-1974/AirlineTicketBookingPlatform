# ✈ SkyBooker — Airline Ticket Booking System (.NET 8)

> **Day 1 Deliverable** — Solution scaffold + Auth Service (fully implemented) + All 8 service shells

---

## 📁 Solution Structure

```
SkyBooker/
├── SkyBooker.sln
│
├── SkyBooker.Auth.API/          ✅ FULLY IMPLEMENTED (Day 1)
│   ├── Entities/User.cs
│   ├── Data/UsersDbContext.cs
│   ├── Repositories/IUserRepository.cs
│   ├── Repositories/UserRepository.cs
│   ├── Services/IAuthService.cs
│   ├── Services/AuthService.cs
│   ├── Controllers/AuthController.cs
│   ├── DTOs/AuthDtos.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── SkyBooker.Flight.API/        🔲 Shell — Implement Day 3
├── SkyBooker.Seat.API/          🔲 Shell — Implement Day 4
├── SkyBooker.Booking.API/       🔲 Shell — Implement Day 5
├── SkyBooker.Passenger.API/     🔲 Shell — Implement Day 6
├── SkyBooker.Payment.API/       🔲 Shell — Implement Day 6
├── SkyBooker.Notification.API/  🔲 Shell — Implement Day 7
├── SkyBooker.Airline.API/       🔲 Shell — Implement Day 2
└── SkyBooker.Web/               🔲 Shell — Implement Day 8-9
```

---

## ⚡ Quick Start (Day 1 Setup)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or Docker)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) / [VS Code](https://code.visualstudio.com/) + C# Dev Kit

---

### Step 1 — Start SQL Server via Docker (easiest)

```bash
docker run -e "ACCEPT_EULA=Y" \
           -e "SA_PASSWORD=YourStrong@Passw0rd" \
           -p 1433:1433 \
           --name skybooker-sql \
           -d mcr.microsoft.com/mssql/server:2022-latest
```

---

### Step 2 — Restore NuGet packages

```bash
cd SkyBooker
dotnet restore SkyBooker.sln
```

---

### Step 3 — Update connection string

Open `SkyBooker.Auth.API/appsettings.json` and set your SQL Server details:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=SkyBookerAuthDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
}
```

---

### Step 4 — Install EF Core tools (if not already installed)

```bash
dotnet tool install --global dotnet-ef
```

---

### Step 5 — Create and run EF Core migrations

```bash
cd SkyBooker.Auth.API

# Create initial migration
dotnet ef migrations add InitialCreate --output-dir Migrations

# Apply migration (creates SkyBookerAuthDb + users table)
dotnet ef database update
```

---

### Step 6 — Run the Auth API

```bash
dotnet run --project SkyBooker.Auth.API
```

Open Swagger UI: **http://localhost:5001** (or whatever port Kestrel assigns)

---

## 🔑 Auth API Endpoints

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| POST | `/api/auth/register` | Register new user | None |
| POST | `/api/auth/login` | Login → returns JWT | None |
| POST | `/api/auth/google` | Google OAuth login | None |
| POST | `/api/auth/logout` | Logout | JWT |
| POST | `/api/auth/refresh` | Refresh JWT token | None |
| GET | `/api/auth/profile` | Get my profile | JWT |
| PUT | `/api/auth/profile` | Update my profile | JWT |
| PUT | `/api/auth/password` | Change password | JWT |
| DELETE | `/api/auth/deactivate/{id}` | Deactivate user | Admin |
| GET | `/api/auth/users` | Get all users | Admin |
| GET | `/api/auth/users/{id}` | Get user by ID | Admin |

---

## 🧪 Test with Swagger — Day 1 Verification

### 1. Register a Passenger
```json
POST /api/auth/register
{
  "fullName": "Rahul Sharma",
  "email": "rahul@example.com",
  "password": "Password@123",
  "confirmPassword": "Password@123",
  "phone": "9876543210",
  "role": "PASSENGER"
}
```

### 2. Login and get JWT
```json
POST /api/auth/login
{
  "email": "rahul@example.com",
  "password": "Password@123"
}
```
Copy the `token` from response.

### 3. Click "Authorize" in Swagger → paste: `Bearer <your-token>`

### 4. Get Profile
```
GET /api/auth/profile
```
Should return user details (no passwordHash in response ✅)

### 5. Update Profile
```json
PUT /api/auth/profile
{
  "fullName": "Rahul Kumar Sharma",
  "phone": "9876543210",
  "passportNumber": "L1234567",
  "nationality": "Indian"
}
```

---

## 🏗️ Clean Architecture — 4 Layers per Service

```
SkyBooker.Auth.API/
│
├── Entities/           ← EF Core entity classes ([Table], [Key], [Index])
│   └── User.cs        ← User entity with UserRoles + AuthProviders constants
│
├── Data/               ← DbContext (one per microservice)
│   └── UsersDbContext.cs ← DbSet<User>, OnModelCreating config, auto UpdatedAt
│
├── Repositories/       ← Data access layer (Interface + Implementation)
│   ├── IUserRepository.cs  ← Interface with 12 async methods
│   └── UserRepository.cs   ← EF Core implementation (AsNoTracking on reads)
│
├── Services/           ← Business logic layer (Interface + Implementation)
│   ├── IAuthService.cs     ← 12 method contract
│   └── AuthService.cs      ← JWT generation, PasswordHasher, token validation
│
├── Controllers/        ← HTTP layer (inherits ControllerBase)
│   └── AuthController.cs   ← 11 endpoints with [Authorize], [ProducesResponseType]
│
├── DTOs/               ← Request + Response data transfer objects
│   └── AuthDtos.cs    ← RegisterDto, LoginDto, UpdateProfileDto, AuthResponseDto...
│
├── Migrations/         ← EF Core auto-generated (dotnet ef migrations add)
│
├── Program.cs          ← DI registration, JWT config, middleware pipeline
└── appsettings.json    ← DB connection, JWT secret, Google OAuth config
```

---

## 📦 NuGet Packages Used (Auth.API)

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore` | 8.0.0 | ORM + DbContext |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.0 | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.0 | `dotnet ef` CLI |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 | JWT middleware |
| `Microsoft.AspNetCore.Authentication.Google` | 8.0.0 | Google OAuth2 |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.0.0 | PasswordHasher\<T\> |
| `Swashbuckle.AspNetCore` | 6.5.0 | Swagger/OpenAPI UI |
| `Serilog.AspNetCore` | 8.0.0 | Structured logging |

---

## 🔐 Security Notes

| Feature | Implementation |
|---------|---------------|
| Password Hashing | `PasswordHasher<User>` — PBKDF2 + HMAC-SHA256 (ASP.NET Core Identity) |
| JWT Signing | HMAC-SHA256 with 32+ char secret from `appsettings.json` |
| Token Expiry | 24 hours (`JwtSettings:ExpiryHours`) — configurable |
| Role Claims | `ClaimTypes.Role` in JWT → `[Authorize(Roles="ADMIN")]` on endpoints |
| No PasswordHash in API | `UserProfileDto` never includes `PasswordHash` field |
| CORS | Configured for `SkyBooker.Web` origin only |

> ⚠️ **IMPORTANT**: Move `JwtSettings:Secret` to **Azure Key Vault** before production deployment. Never commit real secrets to Git!

---

## 📅 10-Day Build Schedule

| Day | Service | Status |
|-----|---------|--------|
| **Day 1** | Solution Setup + Auth.API | ✅ Done |
| Day 2 | Auth complete + Airline.API | 🔲 |
| Day 3 | Flight.API | 🔲 |
| Day 4 | Seat.API (ConcurrencyToken) | 🔲 |
| Day 5 | Booking.API (Transactions + PNR) | 🔲 |
| Day 6 | Passenger.API + Payment.API | 🔲 |
| Day 7 | Notification.API + BackgroundServices | 🔲 |
| Day 8 | SkyBooker.Web — Customer UI | 🔲 |
| Day 9 | SkyBooker.Web — Staff + Admin UI | 🔲 |
| Day 10 | Testing + Docker + CI/CD | 🔲 |

---

## 🐞 Common Issues

### "Cannot connect to SQL Server"
→ Make sure Docker container is running: `docker ps`
→ Check SA_PASSWORD meets SQL Server complexity requirements (uppercase + number + special char)

### "dotnet ef not found"
→ Run: `dotnet tool install --global dotnet-ef`
→ Restart terminal

### "Invalid token" on Swagger
→ Click Authorize → Enter: `Bearer eyJhbGc...` (with the word "Bearer " before the token)

### Migration fails with "Cannot open database"
→ Run `docker start skybooker-sql` if container was stopped

---

*SkyBooker Platform v1.0 | .NET 8 / ASP.NET Core / Entity Framework Core*
