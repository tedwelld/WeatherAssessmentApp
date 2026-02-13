import { Injectable } from '@angular/core';
import { BehaviorSubject, EMPTY, Observable, catchError, finalize, forkJoin, map, switchMap, tap } from 'rxjs';
import { WeatherApiService } from '../services/weather-api.service';
import { NotificationCenterService } from '../services/notification-center.service';
import {
  CreateLocationRequest,
  CurrentWeatherDto,
  LocationDto,
  SyncOperationDto,
  WeatherTimelineDto,
  NextFiveDayForecastDto,
  UpdateLocationRequest,
  UpdateUserPreferencesRequest,
  UserPreferencesDto,
  WeatherForecastDto
} from '../models';

@Injectable({ providedIn: 'root' })
export class WeatherStoreService {
  private readonly locationsSubject = new BehaviorSubject<LocationDto[]>([]);
  private readonly currentWeatherSubject = new BehaviorSubject<CurrentWeatherDto[]>([]);
  private readonly preferencesSubject = new BehaviorSubject<UserPreferencesDto | null>(null);
  private readonly syncHistorySubject = new BehaviorSubject<SyncOperationDto[]>([]);
  private readonly isLoadingSubject = new BehaviorSubject<boolean>(false);
  private readonly errorSubject = new BehaviorSubject<string | null>(null);
  private readonly knownSyncOperationIds = new Set<number>();

  readonly locations$ = this.locationsSubject.asObservable();
  readonly currentWeather$ = this.currentWeatherSubject.asObservable();
  readonly preferences$ = this.preferencesSubject.asObservable();
  readonly syncHistory$ = this.syncHistorySubject.asObservable();
  readonly isLoading$ = this.isLoadingSubject.asObservable();
  readonly error$ = this.errorSubject.asObservable();

  constructor(
    private readonly api: WeatherApiService,
    private readonly notifications: NotificationCenterService
  ) {}

  loadInitialData(): Observable<void> {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    return forkJoin({
      locations: this.api.getLocations(),
      weather: this.api.getTrackedCurrentWeather(),
      preferences: this.api.getPreferences(),
      syncHistory: this.api.getSyncHistory()
    }).pipe(
      tap(({ locations, weather, preferences, syncHistory }) => {
        this.locationsSubject.next(locations);
        this.currentWeatherSubject.next(weather);
        this.preferencesSubject.next(preferences);
        this.setSyncHistory(syncHistory, false);
      }),
      map(() => void 0),
      catchError((error) => this.handleError(error)),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  addLocation(request: CreateLocationRequest): Observable<void> {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    return this.api.createLocation(request).pipe(
      switchMap(() => this.refreshState()),
      map(() => void 0),
      catchError((error) => this.handleError(error)),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  updateLocation(id: number, request: UpdateLocationRequest): Observable<void> {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    return this.api.updateLocation(id, request).pipe(
      switchMap(() => this.refreshState()),
      map(() => void 0),
      catchError((error) => this.handleError(error)),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  deleteLocation(id: number): Observable<void> {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    return this.api.deleteLocation(id).pipe(
      switchMap(() => this.refreshState()),
      map(() => void 0),
      catchError((error) => this.handleError(error)),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  refreshAll(): Observable<void> {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    return this.api.refreshAll().pipe(
      switchMap(() => this.refreshState()),
      map(() => void 0),
      catchError((error) => this.handleError(error)),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  refreshLocation(id: number): Observable<void> {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    return this.api.refreshLocation(id).pipe(
      switchMap(() => this.refreshState()),
      map(() => void 0),
      catchError((error) => this.handleError(error)),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  updatePreferences(request: UpdateUserPreferencesRequest): Observable<void> {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    return this.api.updatePreferences(request).pipe(
      tap((preferences) => this.preferencesSubject.next(preferences)),
      map(() => void 0),
      catchError((error) => this.handleError(error)),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  getForecast(locationId: number): Observable<WeatherForecastDto> {
    this.errorSubject.next(null);
    return this.api.getForecastByLocation(locationId).pipe(catchError((error) => this.handleError(error)));
  }

  getTimeline(locationId: number): Observable<WeatherTimelineDto> {
    this.errorSubject.next(null);
    return this.api.getTimelineByLocation(locationId).pipe(catchError((error) => this.handleError(error)));
  }

  getNextFiveDays(locationId: number): Observable<NextFiveDayForecastDto> {
    this.errorSubject.next(null);
    return this.api.getNextFiveDaysByLocation(locationId).pipe(catchError((error) => this.handleError(error)));
  }

  getWeatherForLocation(locationId: number): Observable<CurrentWeatherDto | null> {
    return this.currentWeather$.pipe(
      map((weatherItems) => weatherItems.find((item) => item.locationId === locationId) ?? null)
    );
  }

  private refreshState(): Observable<void> {
    // Reload all dashboard slices after any mutating command to keep UI consistent.
    return forkJoin({
      locations: this.api.getLocations(),
      weather: this.api.getTrackedCurrentWeather(),
      preferences: this.api.getPreferences(),
      syncHistory: this.api.getSyncHistory()
    }).pipe(
      tap(({ locations, weather, preferences, syncHistory }) => {
        this.locationsSubject.next(locations);
        this.currentWeatherSubject.next(weather);
        this.preferencesSubject.next(preferences);
        this.setSyncHistory(syncHistory, true);
      }),
      map(() => void 0),
      catchError((error) => this.handleError(error))
    );
  }

  private handleError(error: any): Observable<never> {
    const message =
      error?.error?.message ||
      error?.error?.Message ||
      error?.message ||
      'Unable to complete request.';
    this.errorSubject.next(message);
    this.notifications.notify(message, 'error', { showToast: true });
    return EMPTY;
  }

  private setSyncHistory(syncHistory: SyncOperationDto[], announceUpdates: boolean): void {
    this.syncHistorySubject.next(syncHistory);

    // Track newly seen sync operations so we only notify once per operation ID.
    const newItems = syncHistory
      .filter((item) => !this.knownSyncOperationIds.has(item.id))
      .sort((a, b) => a.id - b.id);

    if (announceUpdates) {
      newItems.forEach((item) => {
        this.notifications.notify(this.formatSyncNotification(item), 'info');
      });
    }

    newItems.forEach((item) => this.knownSyncOperationIds.add(item.id));
  }

  private formatSyncNotification(item: SyncOperationDto): string {
    if (item.type === 'RefreshAll') {
      return `Sync updated ${item.refreshedLocations} location(s), ${item.snapshotsCreated} snapshot(s) changed.`;
    }

    return `Location sync completed for ${item.locationDisplayName}.`;
  }
}
