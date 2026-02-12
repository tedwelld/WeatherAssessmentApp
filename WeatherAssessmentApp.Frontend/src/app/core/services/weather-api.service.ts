import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateLocationRequest,
  CurrentWeatherDto,
  LocationDto,
  NextFiveDayForecastDto,
  WeatherTimelineDto,
  UpdateLocationRequest,
  UpdateUserPreferencesRequest,
  UserPreferencesDto,
  WeatherForecastDto
} from '../models';

@Injectable({ providedIn: 'root' })
export class WeatherApiService {
  private readonly apiBaseUrl = 'http://localhost:5044/api';

  constructor(private readonly http: HttpClient) {}

  getLocations(): Observable<LocationDto[]> {
    return this.http.get<LocationDto[]>(`${this.apiBaseUrl}/locations`);
  }

  createLocation(payload: CreateLocationRequest): Observable<LocationDto> {
    return this.http.post<LocationDto>(`${this.apiBaseUrl}/locations`, payload);
  }

  updateLocation(id: number, payload: UpdateLocationRequest): Observable<LocationDto> {
    return this.http.put<LocationDto>(`${this.apiBaseUrl}/locations/${id}`, payload);
  }

  deleteLocation(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/locations/${id}`);
  }

  refreshLocation(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiBaseUrl}/locations/${id}/refresh`, {});
  }

  refreshAll(): Observable<{ refreshedLocations: number; refreshedAtUtc: string }> {
    return this.http.post<{ refreshedLocations: number; refreshedAtUtc: string }>(`${this.apiBaseUrl}/sync/refresh-all`, {});
  }

  getTrackedCurrentWeather(): Observable<CurrentWeatherDto[]> {
    return this.http.get<CurrentWeatherDto[]>(`${this.apiBaseUrl}/weather/current`);
  }

  getForecastByLocation(locationId: number): Observable<WeatherForecastDto> {
    return this.http.get<WeatherForecastDto>(`${this.apiBaseUrl}/weather/forecast/${locationId}`);
  }

  getTimelineByLocation(locationId: number): Observable<WeatherTimelineDto> {
    return this.http.get<WeatherTimelineDto>(`${this.apiBaseUrl}/weather/timeline/${locationId}`);
  }

  getNextFiveDaysByLocation(locationId: number): Observable<NextFiveDayForecastDto> {
    return this.http.get<NextFiveDayForecastDto>(`${this.apiBaseUrl}/weather/next-five-days/${locationId}`);
  }

  getPreferences(): Observable<UserPreferencesDto> {
    return this.http.get<UserPreferencesDto>(`${this.apiBaseUrl}/preferences`);
  }

  updatePreferences(payload: UpdateUserPreferencesRequest): Observable<UserPreferencesDto> {
    return this.http.put<UserPreferencesDto>(`${this.apiBaseUrl}/preferences`, payload);
  }

  getCurrentByCity(city: string, country?: string): Observable<CurrentWeatherDto> {
    let params = new HttpParams().set('city', city);
    if (country) {
      params = params.set('country', country);
    }

    return this.http.get<CurrentWeatherDto>(`${this.apiBaseUrl}/weather/by-city/current`, { params });
  }
}
