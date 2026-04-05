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