# Weather Data Integration Platform

Production-ready full-stack weather platform with Clean Architecture backend and Angular frontend.

## Tech Stack

- Backend: ASP.NET Core Web API (.NET 8), EF Core, SQLite/SQL Server, OpenWeatherMap integration
- Architecture: `Domain`, `Application`, `Infrastructure`, `Web`
- Frontend: Angular 21 (standalone), responsive UI, RxJS-based state management
- Testing: xUnit + Moq + FluentAssertions
- Ops: Docker + docker-compose, background sync, in-memory API caching, API rate limiting

## Solution Structure

```text
WeatherAssessmentApp.Domain
WeatherAssessmentApp.Application
WeatherAssessmentApp.Infrastructure
WeatherAssessmentApp.Web
WeatherAssessmentApp.Application.Tests
WeatherAssessmentApp.Frontend
```

## Features Implemented

- Fetch current weather by city
- Fetch 5-day forecast by city and by tracked location
- Robust external API error handling (invalid city, rate-limit, network, timeouts)
- Persistence model:
  - `Locations`
  - `WeatherSnapshots`
  - `UserPreferences`
- CRUD for tracked locations
- Current weather dashboard for tracked cities
- Forecast details per city
- Manual sync per location + sync all
- Background scheduled sync based on user preferences
- Conflict handling:
  - optimistic concurrency token on locations (`RowVersion`)
  - fingerprint-based snapshot writes only when API weather actually changes
- Clean DI setup
- Global exception handling middleware
- EF Core migration included
- Backend unit tests
- Frontend responsive UI with centralized store
- Bonus:
  - Docker support
  - Caching strategy
  - Rate limiting middleware
  - Mobile-friendly UI

## Configuration

Set `OpenWeatherMap:ApiKey` in `WeatherAssessmentApp.Web/appsettings.json` or environment variable:

- `OpenWeatherMap__ApiKey=YOUR_KEY`

Database provider can be switched with:

- `Database__Provider=sqlite` (default)
- `Database__Provider=sqlserver`

## Run Locally

### Backend

```bash
cd WeatherAssessmentApp
dotnet build WeatherAssessmentApp.slnx
dotnet run --project WeatherAssessmentApp.Web
```

API base URL (default launch profile): `http://localhost:5044`

Swagger: `http://localhost:5044/swagger`

### Frontend

```bash
cd WeatherAssessmentApp/WeatherAssessmentApp.Frontend
npm install
npm start
```

Frontend URL: `http://localhost:4200`

## Database Migrations

Initial migration is committed under:

- `WeatherAssessmentApp.Infrastructure/Persistence/Migrations`

To create a new migration:

```bash
cd WeatherAssessmentApp
dotnet ef migrations add <Name> \
  --project WeatherAssessmentApp.Infrastructure \
  --startup-project WeatherAssessmentApp.Web \
  --context WeatherDbContext \
  --output-dir Persistence/Migrations
```

## Run Tests

```bash
cd WeatherAssessmentApp
dotnet test WeatherAssessmentApp.slnx
```

## Docker

From the repository root:

```bash
cd WeatherAssessmentApp
set OPENWEATHERMAP_API_KEY=YOUR_KEY
docker compose up --build
```

- API: `http://localhost:5044`
- Frontend: `http://localhost:4200`

## API Endpoints

- `GET /api/locations`
- `POST /api/locations`
- `PUT /api/locations/{id}`
- `DELETE /api/locations/{id}`
- `POST /api/locations/{id}/refresh`
- `GET /api/weather/current`
- `GET /api/weather/current/{locationId}`
- `GET /api/weather/forecast/{locationId}`
- `GET /api/weather/by-city/current?city=...&country=...`
- `GET /api/weather/by-city/forecast?city=...&country=...`
- `GET /api/preferences`
- `PUT /api/preferences`
- `POST /api/sync/refresh-all`

## Demo Walkthrough

1. Start backend and frontend.
2. Open `http://localhost:4200`.
3. Add a city (for example: `Seattle`, `US`).
4. Observe current weather, sync timestamp, and favorite toggle.
5. Open the city Forecast page.
6. Change units/refresh interval and save preferences.
7. Trigger manual refresh (`Refresh` / `Refresh All`) and verify snapshot updates.
