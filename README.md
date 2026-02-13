# Weather Assessment App

Full-stack weather tracking and forecasting platform with a Clean Architecture backend and Angular frontend.

## 1. System Overview

The system allows users to:
- Add and remove tracked cities.
- View current weather for tracked cities.
- View 5-day forecast data for a selected city.
- Trigger manual weather refresh for one city or all cities.
- View recent sync history.
- Manage user preferences (units and refresh interval).
- Visualize country forecast trends (cards, clustered bars, line charts).
- Export forecast reports to PDF.
- Use an interactive world map for hover weather inspection.

Backend and frontend are decoupled and communicate via REST APIs.

## 2. Tech Stack

### Backend
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQLite / SQL Server
- Swashbuckle (Swagger/OpenAPI)
- Built-in Rate Limiter middleware
- Hosted background service for periodic sync

### Frontend
- Angular 21 (standalone components)
- RxJS
- SCSS
- Leaflet (world map)
- jsPDF + autoTable (PDF export)

### Testing
- xUnit
- Moq
- FluentAssertions

### DevOps
- Docker + docker-compose

## 3. Architecture

The backend follows Clean Architecture with clear boundaries:

- `WeatherAssessmentApp.Domain`
  - Core entities and enums.
  - No infrastructure dependencies.

- `WeatherAssessmentApp.Application`
  - Business use cases, DTOs, service contracts, validation/application exceptions.
  - Depends on `Domain`.

- `WeatherAssessmentApp.Infrastructure`
  - EF Core DbContext, repositories, external API client (OpenWeatherMap), background sync.
  - Depends on `Application` and `Domain`.

- `WeatherAssessmentApp.Web`
  - API controllers, middleware, composition root (`Program.cs`), Swagger, CORS, rate limiting.
  - Depends on `Application` and `Infrastructure`.

- `WeatherAssessmentApp.Frontend`
  - Angular SPA with centralized weather state store and feature pages.

## 4. Core Data Model

Main persisted entities:
- `Location`
  - City, country, coordinates, favorite flag, sync metadata, concurrency token.
- `WeatherSnapshot`
  - Observed weather values and raw source payload.
- `UserPreferences`
  - Units and refresh interval.
- `SyncOperation`
  - Refresh history for operational visibility.

## 5. API Surface (High Level)

### Locations
- `GET /api/locations`
- `GET /api/locations/{id}`
- `POST /api/locations`
- `PUT /api/locations/{id}`
- `DELETE /api/locations/{id}`
- `POST /api/locations/{id}/refresh`

### Weather
- `GET /api/weather/current`
- `GET /api/weather/current/{locationId}`
- `GET /api/weather/forecast/{locationId}`
- `GET /api/weather/timeline/{locationId}`
- `GET /api/weather/next-five-days/{locationId}`
- `GET /api/weather/by-city/current?city=...&country=...&units=metric|imperial`
- `GET /api/weather/by-city/forecast?city=...&country=...&units=metric|imperial`

### Preferences
- `GET /api/preferences`
- `PUT /api/preferences`

### Sync
- `POST /api/sync/refresh-all`
- `GET /api/sync/history?take=20`

Swagger UI is available by default when the backend runs.

## 6. Project Structure

```text
WeatherAssessmentApp.Domain/
WeatherAssessmentApp.Application/
WeatherAssessmentApp.Infrastructure/
WeatherAssessmentApp.Web/
WeatherAssessmentApp.Application.Tests/
WeatherAssessmentApp.Frontend/
docker-compose.yml
```

## 7. Prerequisites

- .NET SDK 8.0+
- Node.js 20+ (or compatible with Angular 21)
- npm 10+
- Optional:
  - SQL Server LocalDB (if using sqlserver provider)
  - Docker Desktop

## 8. Configuration

Backend settings are in:
- `WeatherAssessmentApp.Web/appsettings.json`
- `WeatherAssessmentApp.Web/appsettings.Development.json`

Key settings:
- `Database:Provider` = `sqlite` or `sqlserver`
- `ConnectionStrings:Sqlite`
- `ConnectionStrings:SqlServer`
- `OpenWeatherMap:ApiKey`
- `OpenWeatherMap:BaseUrl`
- `OpenWeatherMap:CacheDurationMinutes`
- `BackgroundSync:Enabled`
- `BackgroundSync:FallbackRefreshIntervalMinutes`

Recommended local environment variable for API key:

```powershell
$env:OpenWeatherMap__ApiKey="YOUR_OPENWEATHERMAP_KEY"
```

Note:
- Avoid committing real API keys to source control.

## 9. How To Start (Local)

### Backend

```powershell
dotnet restore WeatherAssessmentApp.slnx
dotnet build WeatherAssessmentApp.slnx
dotnet run --project WeatherAssessmentApp.Web
```

Default URLs:
- API: `http://localhost:5044`
- Swagger: `http://localhost:5044/swagger`

At startup, backend applies EF migrations and seeds demo locations.

### Frontend

```powershell
cd WeatherAssessmentApp.Frontend
npm install
npm start
```

Default URL:
- SPA: `http://localhost:4200`

The frontend currently points to backend base URL:
- `http://localhost:5044/api`
defined in:
- `WeatherAssessmentApp.Frontend/src/app/core/services/weather-api.service.ts`

## 10. Build

### Backend

```powershell
dotnet build WeatherAssessmentApp.slnx -c Release
```

### Frontend

```powershell
cd WeatherAssessmentApp.Frontend
npm run build
```

Build output:
- `WeatherAssessmentApp.Frontend/dist/WeatherAssessmentApp.Frontend`

## 11. Database and Migrations

Migrations live in:
- `WeatherAssessmentApp.Infrastructure/Persistence/Migrations`

Create a migration:

```powershell
dotnet ef migrations add <MigrationName> `
  --project WeatherAssessmentApp.Infrastructure `
  --startup-project WeatherAssessmentApp.Web `
  --context WeatherDbContext `
  --output-dir Persistence/Migrations
```

Apply migration manually:

```powershell
dotnet ef database update `
  --project WeatherAssessmentApp.Infrastructure `
  --startup-project WeatherAssessmentApp.Web `
  --context WeatherDbContext
```

Note:
- Current startup already calls `Database.MigrateAsync()` in `Program.cs`.

## 12. Testing

Run backend application tests:

```powershell
dotnet test WeatherAssessmentApp.slnx
```

Frontend test command:

```powershell
cd WeatherAssessmentApp.Frontend
npm test
```

## 13. Docker

From repository root:

```powershell
$env:OPENWEATHERMAP_API_KEY="YOUR_OPENWEATHERMAP_KEY"
docker compose up --build
```

Services:
- API: `http://localhost:5044`
- Frontend: `http://localhost:4200`

## 14. Methodology Used

The implementation follows a practical layered methodology:

1. Domain-first modeling
   - Entities and invariants modeled in `Domain`.
2. Use-case driven application services
   - Orchestration in `Application` via interfaces and DTO contracts.
3. Infrastructure adapters
   - EF repositories, external API clients, background jobs in `Infrastructure`.
4. Thin delivery layer
   - Controllers in `Web` expose REST endpoints and rely on service abstractions.
5. Reactive frontend composition
   - Angular services + RxJS store patterns for state and UI synchronization.
6. Incremental validation
   - Migrations, unit tests, and run/build checks used during changes.

## 15. Dependency Summary

### Backend NuGet (key packages)
- `Microsoft.EntityFrameworkCore` 8.x
- `Microsoft.EntityFrameworkCore.Sqlite` 8.x
- `Microsoft.EntityFrameworkCore.SqlServer` 8.x
- `Swashbuckle.AspNetCore` 6.x

### Frontend npm (key packages)
- `@angular/*` 21.x
- `rxjs` 7.x
- `leaflet` 1.9.x
- `jspdf` 4.x
- `jspdf-autotable` 5.x

## 16. Operational Notes

- CORS is currently configured for `http://localhost:4200` in `Program.cs`.
- Rate limiting is enabled globally (fixed window policy).
- Background sync interval is driven by user preferences, with fallback value from configuration.
- Swagger is always enabled in current startup configuration.

## 17. Troubleshooting

### Frontend cannot call backend
- Confirm backend is running on `http://localhost:5044`.
- Confirm browser console for CORS/network errors.
- Confirm `apiBaseUrl` in frontend service matches backend URL.

### No weather data returned
- Validate OpenWeatherMap API key.
- Check backend logs for external API errors or rate limits.
- Try manual sync endpoint from Swagger.

### Migration or database issues
- Confirm selected provider (`sqlite` or `sqlserver`) and matching connection string.
- Re-run migrations using `dotnet ef database update`.

## 18. Recommended Next Improvements

- Move frontend API base URL to environment-based Angular configuration.
- Add integration tests for API controllers.
- Add frontend E2E tests.
- Add CI pipeline (build/test/lint/migration validation).
- Strengthen secrets management (user-secrets, vault, or CI secret store).

