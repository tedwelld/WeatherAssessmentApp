import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, combineLatest, finalize, map, startWith } from 'rxjs';
import {
  CurrentWeatherDto,
  LocationDto,
  NextFiveDayForecastDto,
  TemperatureUnit,
  UserPreferencesDto
} from '../../core/models';
import { WeatherVisualService } from '../../core/services/weather-visual.service';
import { WeatherStoreService } from '../../core/store/weather-store.service';

interface DashboardViewModel {
  locations: LocationDto[];
  weatherByLocation: Record<number, CurrentWeatherDto>;
  countryCards: CountryCardViewModel[];
  countryForecasts: Record<string, CountryForecastState>;
  countryForecastLoading: Record<string, boolean>;
  expandedCountries: Record<string, boolean>;
  preferences: UserPreferencesDto | null;
  loading: boolean;
  error: string | null;
}

interface CountryCardViewModel {
  country: string;
  locations: LocationDto[];
  locationId: number | null;
  locationsCount: number;
  units: TemperatureUnit | undefined;
}

interface CountryForecastState {
  forecast: NextFiveDayForecastDto;
  chart: ForecastChartViewModel;
}

interface ForecastChartViewModel {
  width: number;
  height: number;
  padding: number;
  xAxis: { label: string; x: number }[];
  gridLines: { y: number; label: string }[];
  series: ForecastSeriesViewModel[];
}

interface ForecastSeriesViewModel {
  key: string;
  label: string;
  color: string;
  points: string;
  dots: { x: number; y: number }[];
}

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly store = inject(WeatherStoreService);
  private readonly weatherVisual = inject(WeatherVisualService);

  private readonly countryForecastsSubject = new BehaviorSubject<Record<string, CountryForecastState>>({});
  private readonly countryForecastLoadingSubject = new BehaviorSubject<Record<string, boolean>>({});
  private readonly expandedCountriesSubject = new BehaviorSubject<Record<string, boolean>>({});

  readonly addCityForm = this.fb.group({
    city: ['', [Validators.required, Validators.maxLength(128)]],
    country: ['', [Validators.maxLength(64)]],
    favorite: [false]
  });

  readonly preferencesForm = this.fb.group({
    units: ['metric', Validators.required],
    refreshIntervalMinutes: [30, [Validators.required, Validators.min(5), Validators.max(1440)]]
  });

  readonly vm$ = combineLatest({
    locations: this.store.locations$,
    weather: this.store.currentWeather$,
    preferences: this.store.preferences$,
    loading: this.store.isLoading$,
    error: this.store.error$,
    countryForecasts: this.countryForecastsSubject,
    countryForecastLoading: this.countryForecastLoadingSubject,
    expandedCountries: this.expandedCountriesSubject
  }).pipe(
    map(({ locations, weather, preferences, loading, error, countryForecasts, countryForecastLoading, expandedCountries }) => {
      const weatherByLocation = weather
        .filter((item) => item.locationId !== null)
        .reduce<Record<number, CurrentWeatherDto>>((acc, item) => {
          acc[item.locationId as number] = item;
          return acc;
        }, {});
      const countryCards = this.buildCountryCards(locations, weatherByLocation);

      return {
        locations,
        weatherByLocation,
        countryCards,
        countryForecasts,
        countryForecastLoading,
        expandedCountries,
        preferences,
        loading,
        error
      } satisfies DashboardViewModel;
    })
  );

  ngOnInit(): void {
    this.store.loadInitialData().subscribe();

    this.store.preferences$
      .pipe(startWith(null))
      .pipe(takeUntilDestroyed())
      .subscribe((preferences) => {
        if (!preferences) {
          return;
        }

        this.preferencesForm.patchValue(
          {
            units: preferences.units === 'Imperial' ? 'imperial' : 'metric',
            refreshIntervalMinutes: preferences.refreshIntervalMinutes
          },
          { emitEvent: false }
        );
      });

    combineLatest([this.store.locations$, this.store.currentWeather$])
      .pipe(takeUntilDestroyed())
      .subscribe(([locations, weatherItems]) => {
        const selectedLocation = locations.find((location) => location.isFavorite) ?? locations[0];
        const selectedWeather = selectedLocation
          ? weatherItems.find((weather) => weather.locationId === selectedLocation.id)
          : weatherItems[0];

        if (!selectedWeather) {
          this.weatherVisual.reset();
          return;
        }

        this.weatherVisual.applyFromCurrent(
          selectedWeather.summary,
          selectedWeather.temperature,
          selectedWeather.windSpeed,
          selectedWeather.humidity
        );
      });
  }

  addCity(): void {
    if (this.addCityForm.invalid) {
      this.addCityForm.markAllAsTouched();
      return;
    }

    const formValue = this.addCityForm.getRawValue();
    this.store
      .addLocation({
        city: formValue.city?.trim() ?? '',
        country: formValue.country?.trim() || null,
        isFavorite: formValue.favorite ?? false
      })
      .subscribe(() => this.addCityForm.reset({ city: '', country: '', favorite: false }));
  }

  deleteLocation(locationId: number): void {
    this.store.deleteLocation(locationId).subscribe();
  }

  refreshLocation(locationId: number): void {
    this.store.refreshLocation(locationId).subscribe();
  }

  refreshAll(): void {
    this.store.refreshAll().subscribe();
  }

  toggleFavorite(location: LocationDto): void {
    this.store
      .updateLocation(location.id, {
        isFavorite: !location.isFavorite
      })
      .subscribe();
  }

  savePreferences(): void {
    if (this.preferencesForm.invalid) {
      this.preferencesForm.markAllAsTouched();
      return;
    }

    const formValue = this.preferencesForm.getRawValue();
    this.store
      .updatePreferences({
        units: (formValue.units as 'metric' | 'imperial') ?? 'metric',
        refreshIntervalMinutes: formValue.refreshIntervalMinutes ?? 30
      })
      .subscribe();
  }

  getTempUnit(units: TemperatureUnit | undefined): string {
    return units === 'Imperial' ? 'F' : 'C';
  }

  getWindUnit(units: TemperatureUnit | undefined): string {
    return units === 'Imperial' ? 'mph' : 'm/s';
  }

  formatTimestamp(value: string | null): string {
    if (!value) {
      return 'Never';
    }

    return new Date(value).toLocaleString();
  }

  formatDayLabel(value: string): string {
    return new Date(value).toLocaleDateString(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    });
  }

  getIconUrl(iconCode: string): string {
    return `https://openweathermap.org/img/wn/${iconCode}@2x.png`;
  }

  toggleCountryForecast(card: CountryCardViewModel): void {
    if (!card.locationId) {
      return;
    }

    const key = card.country;
    const expanded = { ...this.expandedCountriesSubject.value, [key]: !this.expandedCountriesSubject.value[key] };
    this.expandedCountriesSubject.next(expanded);

    if (!expanded[key]) {
      return;
    }

    if (this.countryForecastsSubject.value[key]) {
      return;
    }

    this.setCountryLoading(key, true);
    this.store
      .getNextFiveDays(card.locationId)
      .pipe(
        finalize(() => this.setCountryLoading(key, false)),
        takeUntilDestroyed()
      )
      .subscribe({
        next: (forecast) => {
          const chart = this.buildForecastChart(forecast);
          this.countryForecastsSubject.next({
            ...this.countryForecastsSubject.value,
            [key]: { forecast, chart }
          });
        }
      });
  }

  trackByCountry(index: number, item: CountryCardViewModel): string {
    return item.country;
  }

  private setCountryLoading(key: string, isLoading: boolean): void {
    this.countryForecastLoadingSubject.next({
      ...this.countryForecastLoadingSubject.value,
      [key]: isLoading
    });
  }

  private buildCountryCards(
    locations: LocationDto[],
    weatherByLocation: Record<number, CurrentWeatherDto>
  ): CountryCardViewModel[] {
    const buckets = locations.reduce<Record<string, LocationDto[]>>((acc, location) => {
      const key = location.country?.trim().toUpperCase() || 'N/A';
      if (!acc[key]) {
        acc[key] = [];
      }
      acc[key].push(location);
      return acc;
    }, {});

    return Object.entries(buckets)
      .map(([country, countryLocations]) => {
        const preferred = countryLocations.find((item) => item.isFavorite) ?? countryLocations[0];
        const units = preferred ? weatherByLocation[preferred.id]?.units ?? preferred.units : undefined;
        return {
          country,
          locations: countryLocations,
          locationId: preferred?.id ?? null,
          locationsCount: countryLocations.length,
          units
        };
      })
      .sort((a, b) => a.country.localeCompare(b.country));
  }

  private buildForecastChart(forecast: NextFiveDayForecastDto): ForecastChartViewModel {
    const width = 680;
    const height = 240;
    const padding = 44;

    if (forecast.days.length === 0) {
      return {
        width,
        height,
        padding,
        xAxis: [],
        gridLines: [],
        series: []
      };
    }

    const labels = forecast.days.map((day) =>
      new Date(day.dateUtc).toLocaleDateString(undefined, { weekday: 'short' })
    );
    const xStep = labels.length > 1 ? (width - padding * 2) / (labels.length - 1) : 0;

    const tempValues = forecast.days.map((day) => day.temperature);
    const feelsValues = forecast.days.map((day) => day.feelsLike);
    const allValues = [...tempValues, ...feelsValues];
    const minValue = Math.min(...allValues);
    const maxValue = Math.max(...allValues);
    const span = maxValue - minValue || 1;

    const toPoint = (value: number, index: number): { x: number; y: number } => {
      const x = labels.length > 1 ? padding + index * xStep : width / 2;
      const y = height - padding - ((value - minValue) / span) * (height - padding * 2);
      return { x, y };
    };

    const buildSeries = (key: string, label: string, color: string, values: number[]): ForecastSeriesViewModel => {
      const dots = values.map((value, index) => toPoint(value, index));
      const points = dots.map((dot) => `${dot.x.toFixed(1)},${dot.y.toFixed(1)}`).join(' ');
      return { key, label, color, points, dots };
    };

    const series: ForecastSeriesViewModel[] = [
      buildSeries('temp', `Temperature (${this.getTempUnit(forecast.units)})`, '#e07a2f', tempValues),
      buildSeries('feels', `Feels Like (${this.getTempUnit(forecast.units)})`, '#2b8cc4', feelsValues)
    ];

    const gridLines = Array.from({ length: 5 }, (_, index) => {
      const ratio = index / 4;
      const y = padding + ratio * (height - padding * 2);
      const value = maxValue - ratio * span;
      return { y, label: value.toFixed(1) };
    });

    const xAxis = labels.map((label, index) => {
      const x = labels.length > 1 ? padding + index * xStep : width / 2;
      return { label, x };
    });

    return {
      width,
      height,
      padding,
      xAxis,
      gridLines,
      series
    };
  }
}
