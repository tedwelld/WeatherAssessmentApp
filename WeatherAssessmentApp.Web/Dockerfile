FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WeatherAssessmentApp.Domain/WeatherAssessmentApp.Domain.csproj WeatherAssessmentApp.Domain/
COPY WeatherAssessmentApp.Application/WeatherAssessmentApp.Application.csproj WeatherAssessmentApp.Application/
COPY WeatherAssessmentApp.Infrastructure/WeatherAssessmentApp.Infrastructure.csproj WeatherAssessmentApp.Infrastructure/
COPY WeatherAssessmentApp.Web/WeatherAssessmentApp.csproj WeatherAssessmentApp.Web/
RUN dotnet restore WeatherAssessmentApp.Web/WeatherAssessmentApp.csproj

COPY . .
RUN dotnet publish WeatherAssessmentApp.Web/WeatherAssessmentApp.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WeatherAssessmentApp.dll"]
