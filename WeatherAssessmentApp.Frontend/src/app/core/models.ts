export type TemperatureUnit = 'Metric' | 'Imperial';
export type SyncOperationType = 'LocationRefresh' | 'RefreshAll';

export interface LocationDto {
  id: number;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  isFavorite: boolean;
  lastSyncedAtUtc: string | null;
  units: TemperatureUnit;
}

export interface CurrentWeatherDto {
  locationId: number | null;
  city: string;
  country: string;
  temperature: number;
  feelsLike: number;
  humidity: number;
  pressure: number;
  windSpeed: number;
  summary: string;
  iconCode: string;
  observedAtUtc: string;
  units: TemperatureUnit;
  lastSyncedAtUtc: string | null;
}

export interface ForecastItemDto {
  forecastAtUtc: string;
  temperature: number;
  feelsLike: number;
  humidity: number;
  summary: string;
  iconCode: string;
  windSpeed: number;
}

export interface WeatherForecastDto {
  city: string;
  country: string;
  units: TemperatureUnit;
  items: ForecastItemDto[];
}

export interface DailyWeatherPointDto {
  dateUtc: string;
  temperature: number;
  feelsLike: number;
  humidity: number;
  windSpeed: number;
  summary: string;
  iconCode: string;
}

export interface WeatherTimelineDto {
  city: string;
  country: string;
  units: TemperatureUnit;
  previousFiveDays: DailyWeatherPointDto[];
  nextFiveDays: DailyWeatherPointDto[];
}

export interface NextFiveDayForecastDto {
  city: string;
  country: string;
  units: TemperatureUnit;
  days: DailyWeatherPointDto[];
}

export interface UserPreferencesDto {
  id: number;
  units: TemperatureUnit;
  refreshIntervalMinutes: number;
  updatedAtUtc: string;
}

export interface SyncOperationDto {
  id: number;
  type: SyncOperationType;
  locationId: number | null;
  locationDisplayName: string;
  refreshedLocations: number;
  snapshotsCreated: number;
  occurredAtUtc: string;
}

export interface CreateLocationRequest {
  city: string;
  country?: string | null;
  isFavorite?: boolean;
}

export interface UpdateLocationRequest {
  city?: string | null;
  country?: string | null;
  isFavorite?: boolean | null;
}

export interface UpdateUserPreferencesRequest {
  units: 'metric' | 'imperial';
  refreshIntervalMinutes: number;
}

export interface ApiErrorResponse {
  message: string;
  detail: string | null;
  traceId: string;
}
