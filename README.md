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
- SQL Server
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

- Visual Studio Code (required IDE)
- Git
- .NET SDK 8.0+
- Node.js 20+ (or compatible with Angular 21)
- npm 10+
- SQL Server (Developer / Express / LocalDB)
- Optional:
  - Docker Desktop

## 8. Configuration

Backend settings are in:
- `WeatherAssessmentApp.Web/appsettings.json`
- `WeatherAssessmentApp.Web/appsettings.Development.json`

Key settings:
- `Database:Provider` = `sqlserver` (required)
- `ConnectionStrings:SqlServer`
- `OpenWeatherMap:ApiKey`
- `OpenWeatherMap:BaseUrl`
- `OpenWeatherMap:CacheDurationMinutes`
- `BackgroundSync:Enabled`
- `BackgroundSync:FallbackRefreshIntervalMinutes`

SQL Server only requirement:
- This system is configured to run on SQL Server only.
- `Database:Provider` must remain `sqlserver`.
- Ensure `ConnectionStrings:SqlServer` points to your SQL Server instance.

Recommended local environment variable for API key:

```powershell
$env:OpenWeatherMap__ApiKey="YOUR_OPENWEATHERMAP_KEY"
```

Note:
- Avoid committing real API keys to source control.

## 9. Setup Instructions (GitHub + VS Code + CLI/CMD)

### 9.1 Clone from GitHub

Use CMD or PowerShell:

```powershell
git clone <YOUR_GITHUB_REPOSITORY_URL>
cd <YOUR_REPOSITORY_FOLDER>
```

Example:

```powershell
git clone https://github.com/<org-or-user>/WeatherAssessment.git
cd WeatherAssessment
```

### 9.2 Open in Visual Studio Code

From the repository root:

```powershell
code .
```

In VS Code:
- Open the integrated terminal with ``Ctrl+` ``.
- Use two terminals: one for backend and one for frontend.

### 9.3 Navigate folders and run with CLI/CMD

Backend terminal:

```powershell
cd WeatherAssessmentApp
dotnet restore WeatherAssessmentApp.slnx
dotnet build WeatherAssessmentApp.slnx
dotnet run --project WeatherAssessmentApp.Web
```

Frontend terminal:

```powershell
cd WeatherAssessmentApp\WeatherAssessmentApp.Frontend
npm install
npm start
```

### 9.4 Runtime URLs and startup behavior

Backend URLs:
- API: `http://localhost:5044`
- Swagger: `http://localhost:5044/swagger`

At startup, backend applies EF migrations and seeds demo locations.

Frontend URL:
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

### 11.1 Database Design (Structure)

The application uses Entity Framework Core with SQL Server only:
- `Database:Provider=sqlserver`.
- The active connection string is `ConnectionStrings:SqlServer`.

Schema is created from domain entities via `WeatherDbContext`:
- `WeatherAssessmentApp.Infrastructure/Persistence/WeatherDbContext.cs`

#### ER-style Relationship View

```text
UserPreferences (1) ---- (*) Locations (1) ---- (*) WeatherSnapshots
                                  |
                                  +---- (0..*) SyncOperations (LocationId nullable)
```

#### Table Structure

1. `UserPreferences`
- `Id` (PK)
- `Units` (string enum: Metric/Imperial)
- `RefreshIntervalMinutes`
- `CreatedAtUtc`
- `UpdatedAtUtc`

2. `Locations`
- `Id` (PK)
- `City` (required, max 128)
- `Country` (required, max 64)
- `Latitude`, `Longitude`
- `IsFavorite`
- `LastSyncedAtUtc` (nullable)
- `LastWeatherFingerprint` (nullable, max 128)
- `UserPreferencesId` (FK -> `UserPreferences.Id`, delete behavior: `Restrict`)
- `RowVersion` (concurrency token / optimistic concurrency)

3. `WeatherSnapshots`
- `Id` (PK)
- `LocationId` (FK -> `Locations.Id`, delete behavior: `Cascade`)
- `ObservedAtUtc`
- `Temperature`, `FeelsLike` (decimal(8,2))
- `Humidity`, `Pressure`
- `WindSpeed` (decimal(8,2))
- `Summary` (required, max 256)
- `IconCode` (required, max 10)
- `SourcePayload` (required, raw upstream payload)

4. `SyncOperations`
- `Id` (PK)
- `Type` (string enum: `LocationRefresh` | `RefreshAll`)
- `LocationId` (nullable FK -> `Locations.Id`, delete behavior: `SetNull`)
- `LocationDisplayName` (required, max 192)
- `RefreshedLocations`
- `SnapshotsCreated`
- `OccurredAtUtc`

#### Indexes and Constraints

- Unique index on `Locations(City, Country)` to prevent duplicate tracked locations.
- Index on `WeatherSnapshots(LocationId, ObservedAtUtc)` for timeline/forecast reads.
- Index on `SyncOperations(OccurredAtUtc)` for recent activity queries.
- Index on `SyncOperations(LocationId)`.
- Concurrency via `Locations.RowVersion`.

### 11.2 Database Flow (How Data Moves)

1. Startup flow
- `Program.cs` runs `Database.MigrateAsync()` then `SeedDemoLocationsAsync()`.
- This ensures schema is current and default records exist.

2. Add city flow
- API receives `POST /api/locations`.
- `LocationService` checks duplicates (`City+Country`), resolves current weather from provider, creates `Location`, then inserts first `WeatherSnapshot`.
- Transactional save happens via `UnitOfWork`.

3. Refresh flow (single/all)
- Triggered by `POST /api/locations/{id}/refresh` or `POST /api/sync/refresh-all`.
- `WeatherSyncService` fetches current provider data.
- Fingerprint is compared with `LastWeatherFingerprint`.
- New `WeatherSnapshot` is inserted only when weather payload changed.
- A `SyncOperation` audit record is always written.

4. Read flow for dashboard/forecast
- Current tracked weather and forecasts are read from snapshots + provider-backed logic in `WeatherService`.
- Recent sync history is queried from `SyncOperations` ordered by `OccurredAtUtc DESC`.

5. Background sync flow
- `WeatherSyncBackgroundService` runs in loop.
- Reads preference interval from `UserPreferences`.
- Executes refresh-all and writes snapshots/sync history.

### 11.3 Database Layer Organization

- DbContext and mappings:
  - `WeatherAssessmentApp.Infrastructure/Persistence/WeatherDbContext.cs`
- Repositories:
  - `.../Repositories/LocationRepository.cs`
  - `.../Repositories/WeatherSnapshotRepository.cs`
  - `.../Repositories/UserPreferencesRepository.cs`
  - `.../Repositories/SyncOperationRepository.cs`
- Unit of work and exception translation:
  - `WeatherAssessmentApp.Infrastructure/Persistence/UnitOfWork.cs`
- Seed logic:
  - `WeatherAssessmentApp.Infrastructure/Persistence/WeatherDbSeeder.cs`
- Migrations:
  - `WeatherAssessmentApp.Infrastructure/Persistence/Migrations/*`

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

## 14. Brief Architecture Decisions

1. Clean Architecture boundaries
   - Domain, Application, Infrastructure, and Web are split to isolate business rules from framework and infrastructure concerns.
2. SQL Server persistence
   - EF Core with SQL Server is used for transactional consistency, relational querying, and straightforward migrations.
3. Thin API layer
   - Controllers stay lightweight and delegate business workflows to application services.
4. Infrastructure adapters
   - External weather provider integration, persistence repositories, and background sync are implemented behind interfaces.
5. Decoupled frontend
   - Angular frontend consumes REST endpoints and keeps UI state/store concerns separate from backend logic.
6. Operational resilience
   - Startup migrations, seeded demo data, global exception handling, rate limiting, and background synchronization support predictable runtime behavior.

## 15. Dependency Summary

### Backend NuGet (key packages)
- `Microsoft.EntityFrameworkCore` 8.x
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
- Confirm `Database:Provider` is `sqlserver` and `ConnectionStrings:SqlServer` is valid.
- Re-run migrations using `dotnet ef database update`.

## 18. Recommended Next Improvements

- Move frontend API base URL to environment-based Angular configuration.
- Add integration tests for API controllers.
- Add frontend E2E tests.
- Add CI pipeline (build/test/lint/migration validation).
- Strengthen secrets management (user-secrets, vault, or CI secret store).
