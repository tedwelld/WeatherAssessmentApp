import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { AfterViewInit, Component, DestroyRef, ElementRef, OnDestroy, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import * as L from 'leaflet';
import { Observable, Subject, catchError, combineLatest, debounceTime, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { CurrentWeatherDto, LocationDto } from '../../core/models';
import { WeatherStoreService } from '../../core/store/weather-store.service';

type HoverStatus = 'idle' | 'loading' | 'ready' | 'error';

interface HoverWeatherViewModel {
  status: HoverStatus;
  latitude: number | null;
  longitude: number | null;
  locationLabel: string;
  condition: string;
  temperature: number | null;
  humidity: number | null;
  windSpeed: number | null;
  observedAtUtc: string | null;
  message: string;
}

interface OpenMeteoCurrentResponse {
  current?: {
    temperature_2m: number;
    relative_humidity_2m: number;
    wind_speed_10m: number;
    weather_code: number;
    time: string;
  };
}

interface OpenMeteoReverseResponse {
  results?: Array<{
    name?: string;
    admin1?: string;
    country?: string;
  }>;
}

interface HoverLookupResult {
  locationLabel: string;
  condition: string;
  temperature: number;
  humidity: number;
  windSpeed: number;
  observedAtUtc: string;
}

interface SelectedTrackedCityViewModel {
  status: 'idle' | 'ready' | 'missing';
  cityLabel: string;
  condition: string;
  temperature: number | null;
  humidity: number | null;
  windSpeed: number | null;
  observedAtUtc: string | null;
  units: 'Metric' | 'Imperial';
  message: string;
}

@Component({
  selector: 'app-world-weather-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './world-weather-map.component.html',
  styleUrl: './world-weather-map.component.scss'
})
export class WorldWeatherMapComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapHost', { static: true }) private readonly mapHost?: ElementRef<HTMLDivElement>;

  private readonly http = inject(HttpClient);
  private readonly store = inject(WeatherStoreService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly hoverCoordinates = new Subject<{ latitude: number; longitude: number }>();

  private map?: L.Map;
  private hoverMarker?: L.CircleMarker;
  private trackedCityLayer?: L.LayerGroup;
  private selectedLocationId: number | null = null;
  private locationById: Record<number, LocationDto> = {};
  private weatherByLocationId: Record<number, CurrentWeatherDto> = {};

  hoverWeather: HoverWeatherViewModel = {
    status: 'idle',
    latitude: null,
    longitude: null,
    locationLabel: '',
    condition: '',
    temperature: null,
    humidity: null,
    windSpeed: null,
    observedAtUtc: null,
    message: 'Hover over the map to inspect live current conditions.'
  };

  selectedCityWeather: SelectedTrackedCityViewModel = {
    status: 'idle',
    cityLabel: '',
    condition: '',
    temperature: null,
    humidity: null,
    windSpeed: null,
    observedAtUtc: null,
    units: 'Metric',
    message: 'Click a tracked city marker to view city-specific weather.'
  };

  readonly overallFootnotes$ = combineLatest([this.store.locations$, this.store.currentWeather$]).pipe(
    map(([locations, weatherItems]) => this.buildOverallFootnotes(locations, weatherItems))
  );

  private readonly onMapMouseMove = (event: L.LeafletMouseEvent): void => {
    const latitude = event.latlng.lat;
    const longitude = event.latlng.lng;
    this.updateMarker(latitude, longitude);
    this.hoverCoordinates.next({ latitude, longitude });
  };

  ngAfterViewInit(): void {
    if (!this.mapHost) {
      return;
    }

    this.initializeMap();
    this.bindHoverLookup();
    this.bindTrackedCities();
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.off('mousemove', this.onMapMouseMove);
      this.map.remove();
    }
  }

  private initializeMap(): void {
    if (!this.mapHost) {
      return;
    }

    this.map = L.map(this.mapHost.nativeElement, {
      minZoom: 2,
      maxZoom: 7,
      zoomSnap: 0.25,
      worldCopyJump: true
    }).setView([16, 8], 2.1);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 7,
      minZoom: 2,
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(this.map);

    this.hoverMarker = L.circleMarker([16, 8], {
      radius: 6,
      color: '#14638f',
      weight: 2,
      fillColor: '#66c5ef',
      fillOpacity: 0.82,
      opacity: 0
    }).addTo(this.map);

    this.map.on('mousemove', this.onMapMouseMove);
    setTimeout(() => this.map?.invalidateSize(), 0);
  }

  private bindHoverLookup(): void {
    this.hoverCoordinates
      .pipe(
        // Normalize and debounce hover coordinates to avoid excessive API calls while moving mouse.
        map((coordinates) => ({
          latitude: Number(coordinates.latitude.toFixed(2)),
          longitude: Number(coordinates.longitude.toFixed(2))
        })),
        distinctUntilChanged(
          (previous, current) =>
            previous.latitude === current.latitude && previous.longitude === current.longitude
        ),
        debounceTime(250),
        tap((coordinates) => {
          this.hoverWeather = {
            ...this.hoverWeather,
            status: 'loading',
            latitude: coordinates.latitude,
            longitude: coordinates.longitude,
            locationLabel: this.formatCoordinates(coordinates.latitude, coordinates.longitude),
            message: 'Loading forecast snapshot...'
          };
        }),
        switchMap((coordinates) =>
          this.fetchHoverWeather(coordinates.latitude, coordinates.longitude).pipe(
            map((result) => ({ type: 'success' as const, coordinates, result })),
            catchError((error) =>
              of({
                type: 'error' as const,
                coordinates,
                message: this.getFriendlyError(error)
              })
            )
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((result) => {
        if (result.type === 'error') {
          this.hoverWeather = {
            ...this.hoverWeather,
            status: 'error',
            latitude: result.coordinates.latitude,
            longitude: result.coordinates.longitude,
            locationLabel: this.formatCoordinates(result.coordinates.latitude, result.coordinates.longitude),
            condition: '',
            temperature: null,
            humidity: null,
            windSpeed: null,
            observedAtUtc: null,
            message: result.message
          };
          return;
        }

        this.hoverWeather = {
          status: 'ready',
          latitude: result.coordinates.latitude,
          longitude: result.coordinates.longitude,
          locationLabel: result.result.locationLabel,
          condition: result.result.condition,
          temperature: result.result.temperature,
          humidity: result.result.humidity,
          windSpeed: result.result.windSpeed,
          observedAtUtc: result.result.observedAtUtc,
          message: ''
        };
      });
  }

  private bindTrackedCities(): void {
    combineLatest([this.store.locations$, this.store.currentWeather$])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([locations, weatherItems]) => {
        this.locationById = locations.reduce<Record<number, LocationDto>>((acc, location) => {
          acc[location.id] = location;
          return acc;
        }, {});

        this.weatherByLocationId = weatherItems
          .filter((item) => item.locationId !== null)
          .reduce<Record<number, CurrentWeatherDto>>((acc, item) => {
            acc[item.locationId as number] = item;
            return acc;
          }, {});

        this.renderTrackedCityMarkers(locations);

        if (locations.length === 0) {
          this.selectedLocationId = null;
          this.selectedCityWeather = {
            status: 'idle',
            cityLabel: '',
            condition: '',
            temperature: null,
            humidity: null,
            windSpeed: null,
            observedAtUtc: null,
            units: 'Metric',
            message: 'No tracked cities available yet. Add a city to your watchlist first.'
          };
          return;
        }

        const selectedExists = this.selectedLocationId !== null && !!this.locationById[this.selectedLocationId];
        if (!selectedExists) {
          const preferred = locations.find((item) => item.isFavorite) ?? locations[0];
          this.selectedLocationId = preferred.id;
        }

        if (this.selectedLocationId !== null) {
          this.updateSelectedCityState(this.selectedLocationId);
        }
      });
  }

  private renderTrackedCityMarkers(locations: LocationDto[]): void {
    if (!this.map) {
      return;
    }

    // Rebuild marker layer whenever tracked locations change.
    if (this.trackedCityLayer) {
      this.trackedCityLayer.removeFrom(this.map);
    }

    this.trackedCityLayer = L.layerGroup();

    locations.forEach((location) => {
      if (!Number.isFinite(location.latitude) || !Number.isFinite(location.longitude)) {
        return;
      }

      const marker = L.circleMarker([location.latitude, location.longitude], {
        radius: location.isFavorite ? 8 : 7,
        color: '#0f4a6b',
        weight: 2,
        fillColor: location.isFavorite ? '#f49f45' : '#4db2df',
        fillOpacity: 0.92
      });

      marker.bindTooltip(`${location.city}, ${location.country}`, { direction: 'top', offset: [0, -4] });
      marker.on('click', () => {
        this.selectedLocationId = location.id;
        this.updateSelectedCityState(location.id);
      });

      marker.addTo(this.trackedCityLayer as L.LayerGroup);
    });

    this.trackedCityLayer.addTo(this.map);
  }

  private updateSelectedCityState(locationId: number): void {
    const location = this.locationById[locationId];
    if (!location) {
      this.selectedCityWeather = {
        status: 'missing',
        cityLabel: '',
        condition: '',
        temperature: null,
        humidity: null,
        windSpeed: null,
        observedAtUtc: null,
        units: 'Metric',
        message: 'Selected city is no longer available.'
      };
      return;
    }

    const weather = this.weatherByLocationId[locationId];
    if (!weather) {
      this.selectedCityWeather = {
        status: 'missing',
        cityLabel: `${location.city}, ${location.country}`,
        condition: '',
        temperature: null,
        humidity: null,
        windSpeed: null,
        observedAtUtc: null,
        units: location.units,
        message: 'Weather data is not synced for this city yet. Use refresh to load it.'
      };
      return;
    }

    this.selectedCityWeather = {
      status: 'ready',
      cityLabel: `${weather.city}, ${weather.country}`,
      condition: weather.summary,
      temperature: weather.temperature,
      humidity: weather.humidity,
      windSpeed: weather.windSpeed,
      observedAtUtc: weather.observedAtUtc,
      units: weather.units,
      message: ''
    };
  }

  private fetchHoverWeather(latitude: number, longitude: number): Observable<HoverLookupResult> {
    const weatherParams = new HttpParams()
      .set('latitude', latitude.toString())
      .set('longitude', longitude.toString())
      .set('current', 'temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code')
      .set('timezone', 'auto');

    return this.http
      .get<OpenMeteoCurrentResponse>('https://api.open-meteo.com/v1/forecast', { params: weatherParams })
      .pipe(
        map((response) => {
          if (!response.current) {
            throw new Error('No weather data is available for this location right now.');
          }

          return response.current;
        }),
        switchMap((current) =>
          this.lookupLocationLabel(latitude, longitude).pipe(
            map((locationLabel) => ({
              locationLabel,
              condition: this.getWeatherCodeDescription(current.weather_code),
              temperature: current.temperature_2m,
              humidity: current.relative_humidity_2m,
              windSpeed: current.wind_speed_10m,
              observedAtUtc: current.time
            }))
          )
        )
      );
  }

  private lookupLocationLabel(latitude: number, longitude: number): Observable<string> {
    const reverseParams = new HttpParams()
      .set('latitude', latitude.toString())
      .set('longitude', longitude.toString())
      .set('language', 'en')
      .set('count', '1');

    return this.http
      .get<OpenMeteoReverseResponse>('https://geocoding-api.open-meteo.com/v1/reverse', {
        params: reverseParams
      })
      .pipe(
        map((response) => {
          const item = response.results?.[0];
          if (!item) {
            return this.formatCoordinates(latitude, longitude);
          }

          const sections = [item.name, item.admin1, item.country].filter(
            (value): value is string => typeof value === 'string' && value.trim().length > 0
          );

          return sections.length > 0 ? sections.join(', ') : this.formatCoordinates(latitude, longitude);
        }),
        catchError(() => of(this.formatCoordinates(latitude, longitude)))
      );
  }

  private updateMarker(latitude: number, longitude: number): void {
    if (!this.hoverMarker) {
      return;
    }

    this.hoverMarker.setLatLng([latitude, longitude]);
    this.hoverMarker.setStyle({ opacity: 1, fillOpacity: 0.82 });
  }

  private buildOverallFootnotes(locations: LocationDto[], weatherItems: CurrentWeatherDto[]): string[] {
    const trackedWeather = weatherItems.filter((item) => item.locationId !== null);
    if (trackedWeather.length === 0) {
      return [
        'No tracked weather snapshots are available yet. Add cities and sync data to generate overall trends.',
        'Once data is available, this section summarizes temperature, humidity, wind, and last observed updates.'
      ];
    }

    const units = trackedWeather[0]?.units ?? 'Metric';
    const temperatureUnit = units === 'Imperial' ? 'F' : 'C';
    const windUnit = units === 'Imperial' ? 'mph' : 'm/s';
    const totalCountries = new Set(
      locations.map((location) => (location.country?.trim() || 'N/A').toUpperCase())
    ).size;

    const averageTemperature =
      trackedWeather.reduce((total, item) => total + item.temperature, 0) / trackedWeather.length;
    const averageHumidity =
      trackedWeather.reduce((total, item) => total + item.humidity, 0) / trackedWeather.length;
    const averageWind = trackedWeather.reduce((total, item) => total + item.windSpeed, 0) / trackedWeather.length;

    const warmest = trackedWeather.reduce((current, candidate) =>
      candidate.temperature > current.temperature ? candidate : current
    );
    const windiest = trackedWeather.reduce((current, candidate) =>
      candidate.windSpeed > current.windSpeed ? candidate : current
    );
    const latestObservation = trackedWeather.reduce((current, candidate) =>
      new Date(candidate.observedAtUtc).getTime() > new Date(current.observedAtUtc).getTime() ? candidate : current
    );

    return [
      `Tracking ${trackedWeather.length} city snapshot(s) across ${totalCountries} country group(s).`,
      `Average now: ${averageTemperature.toFixed(1)} deg ${temperatureUnit}, humidity ${Math.round(
        averageHumidity
      )}%, wind ${averageWind.toFixed(1)} ${windUnit}.`,
      `Warmest tracked city: ${warmest.city}, ${warmest.country} at ${warmest.temperature.toFixed(
        1
      )} deg ${temperatureUnit}.`,
      `Highest wind currently: ${windiest.city}, ${windiest.country} at ${windiest.windSpeed.toFixed(
        1
      )} ${windUnit}.`,
      `Latest observed backend snapshot: ${new Date(latestObservation.observedAtUtc).toLocaleString()}.`
    ];
  }

  private getFriendlyError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return 'Network error while fetching map weather. Check your connection and retry.';
      }

      if (error.status === 429) {
        return 'Map weather API rate limit reached. Pause briefly and hover again.';
      }

      if (error.status >= 500) {
        return 'Weather service is temporarily unavailable. Try again shortly.';
      }
    }

    if (error instanceof Error && error.message) {
      return error.message;
    }

    return 'Unable to load weather data for this point.';
  }

  private formatCoordinates(latitude: number, longitude: number): string {
    const latLabel = `${Math.abs(latitude).toFixed(2)}${latitude >= 0 ? 'N' : 'S'}`;
    const lonLabel = `${Math.abs(longitude).toFixed(2)}${longitude >= 0 ? 'E' : 'W'}`;
    return `${latLabel}, ${lonLabel}`;
  }

  private getWeatherCodeDescription(code: number): string {
    switch (code) {
      case 0:
        return 'Clear sky';
      case 1:
      case 2:
      case 3:
        return 'Partly cloudy';
      case 45:
      case 48:
        return 'Fog';
      case 51:
      case 53:
      case 55:
      case 56:
      case 57:
        return 'Drizzle';
      case 61:
      case 63:
      case 65:
      case 66:
      case 67:
      case 80:
      case 81:
      case 82:
        return 'Rain';
      case 71:
      case 73:
      case 75:
      case 77:
      case 85:
      case 86:
        return 'Snow';
      case 95:
      case 96:
      case 99:
        return 'Thunderstorm';
      default:
        return 'Variable weather';
    }
  }

  getSelectedTempUnit(units: 'Metric' | 'Imperial'): string {
    return units === 'Imperial' ? 'F' : 'C';
  }

  getSelectedWindUnit(units: 'Metric' | 'Imperial'): string {
    return units === 'Imperial' ? 'mph' : 'm/s';
  }
}
