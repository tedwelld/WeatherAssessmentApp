import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, combineLatest, finalize, firstValueFrom, map, startWith } from 'rxjs';
import {
  CurrentWeatherDto,
  DailyWeatherPointDto,
  LocationDto,
  NextFiveDayForecastDto,
  SyncOperationDto,
  TemperatureUnit,
  UserPreferencesDto
} from '../../core/models';
import { NotificationCenterService } from '../../core/services/notification-center.service';
import { PdfExportService } from '../../core/services/pdf-export.service';
import { WeatherVisualService } from '../../core/services/weather-visual.service';
import { WeatherStoreService } from '../../core/store/weather-store.service';

type CountryMetricKey = 'temperature' | 'humidity' | 'windSpeed';

interface DashboardViewModel {
  locations: LocationDto[];
  weatherByLocation: Record<number, CurrentWeatherDto>;
  countryCards: CountryCardViewModel[];
  countryForecasts: Record<string, CountryForecastState>;
  countryForecastLoading: Record<string, boolean>;
  expandedCountries: Record<string, boolean>;
  selectedCountryMetric: Partial<Record<string, CountryMetricKey>>;
  syncHistory: SyncOperationDto[];
  exportingPdf: boolean;
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
  barChart: CountryClusteredBarChartViewModel;
  lineCharts: Record<CountryMetricKey, SingleMetricLineChartViewModel>;
}

interface CountryClusteredBarChartViewModel {
  width: number;
  height: number;
  padding: number;
  yAxis: { y: number; label: string }[];
  groups: CountryBarGroupViewModel[];
  legend: { label: string; color: string }[];
}

interface CountryBarGroupViewModel {
  label: string;
  labelX: number;
  bars: CountryBarViewModel[];
}

interface CountryBarViewModel {
  metric: CountryMetricKey;
  color: string;
  valueLabel: string;
  x: number;
  y: number;
  width: number;
  height: number;
}

interface SingleMetricLineChartViewModel {
  metric: CountryMetricKey;
  label: string;
  axisTitle: string;
  color: string;
  width: number;
  height: number;
  padding: number;
  xAxis: { label: string; x: number }[];
  yAxis: { y: number; label: string }[];
  points: string;
  dots: { x: number; y: number; valueLabel: string }[];
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
  private readonly notifications = inject(NotificationCenterService);
  private readonly pdfExport = inject(PdfExportService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly countryForecastsSubject = new BehaviorSubject<Record<string, CountryForecastState>>({});
  private readonly countryForecastLoadingSubject = new BehaviorSubject<Record<string, boolean>>({});
  private readonly expandedCountriesSubject = new BehaviorSubject<Record<string, boolean>>({});
  private readonly selectedCountryMetricSubject = new BehaviorSubject<Partial<Record<string, CountryMetricKey>>>({});
  private readonly exportingPdfSubject = new BehaviorSubject<boolean>(false);
  private currentLocations: LocationDto[] = [];
  private currentWeatherByLocation: Record<number, CurrentWeatherDto> = {};

  private readonly metricPalette: Record<CountryMetricKey, string> = {
    temperature: '#e07a2f',
    humidity: '#2b8cc4',
    windSpeed: '#4a9d5b'
  };

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
    syncHistory: this.store.syncHistory$,
    exportingPdf: this.exportingPdfSubject,
    loading: this.store.isLoading$,
    error: this.store.error$,
    countryForecasts: this.countryForecastsSubject,
    countryForecastLoading: this.countryForecastLoadingSubject,
    expandedCountries: this.expandedCountriesSubject,
    selectedCountryMetric: this.selectedCountryMetricSubject
  }).pipe(
    map(
      ({
        locations,
        weather,
        preferences,
        syncHistory,
        exportingPdf,
        loading,
        error,
        countryForecasts,
        countryForecastLoading,
        expandedCountries,
        selectedCountryMetric
      }) => {
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
        selectedCountryMetric,
        syncHistory,
        exportingPdf,
        preferences,
        loading,
        error
      } satisfies DashboardViewModel;
      }
    )
  );

  ngOnInit(): void {
    this.store.loadInitialData().subscribe();

    this.store.preferences$
      .pipe(startWith(null))
      .pipe(takeUntilDestroyed(this.destroyRef))
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
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([locations, weatherItems]) => {
        this.currentLocations = locations;
        this.currentWeatherByLocation = weatherItems
          .filter((item) => item.locationId !== null)
          .reduce<Record<number, CurrentWeatherDto>>((acc, item) => {
            acc[item.locationId as number] = item;
            return acc;
          }, {});

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
    const previousCountries = this.getCountrySet(this.currentLocations);
    const cityName = formValue.city?.trim() ?? '';
    this.store
      .addLocation({
        city: cityName,
        country: formValue.country?.trim() || null,
        isFavorite: formValue.favorite ?? false
      })
      .subscribe(() => {
        this.addCityForm.reset({ city: '', country: '', favorite: false });
        this.notifications.notify(`City added to watchlist: ${cityName}`, 'success');

        const currentCountries = this.getCountrySet(this.currentLocations);
        const addedCountries = this.getCountryDifference(currentCountries, previousCountries);
        if (addedCountries.length > 0) {
          this.notifications.notify(`New country added: ${addedCountries.join(', ')}`, 'success', { showToast: true });
        }
      });
  }

  deleteLocation(locationId: number): void {
    const previousCountries = this.getCountrySet(this.currentLocations);
    const location = this.currentLocations.find((item) => item.id === locationId);
    this.store.deleteLocation(locationId).subscribe(() => {
      if (location) {
        this.notifications.notify(`City removed from watchlist: ${location.city}, ${location.country}`, 'warning');
      }

      const currentCountries = this.getCountrySet(this.currentLocations);
      const removedCountries = this.getCountryDifference(previousCountries, currentCountries);
      if (removedCountries.length > 0) {
        this.notifications.notify(`Country removed: ${removedCountries.join(', ')}`, 'warning', { showToast: true });
      }
    });
  }

  refreshLocation(locationId: number): void {
    this.store.refreshLocation(locationId).subscribe(() => {
      this.notifications.notify('Location weather refreshed.', 'info');
    });
  }

  refreshAll(): void {
    this.store.refreshAll().subscribe(() => {
      this.notifications.notify('All tracked locations refreshed.', 'info', { showToast: true });
    });
  }

  toggleFavorite(location: LocationDto): void {
    this.store
      .updateLocation(location.id, {
        isFavorite: !location.isFavorite
      })
      .subscribe(() => {
        this.notifications.notify(
          !location.isFavorite ? `Marked favorite: ${location.city}` : `Removed favorite: ${location.city}`,
          'info'
        );
      });
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
      .subscribe(() => {
        this.notifications.notify('Preferences updated.', 'success');
      });
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

  getSyncOperationTypeLabel(type: SyncOperationDto['type']): string {
    return type === 'RefreshAll' ? 'Refresh all' : 'Location refresh';
  }

  getIconUrl(iconCode: string): string {
    return `https://openweathermap.org/img/wn/${iconCode}@2x.png`;
  }

  toggleCountryCard(card: CountryCardViewModel): void {
    if (!card.locationId) {
      return;
    }

    const key = card.country;
    const wasExpanded = this.expandedCountriesSubject.value[key] ?? false;
    if (wasExpanded) {
      this.expandedCountriesSubject.next({});
      return;
    }

    // Accordion behavior: keep only one country forecast panel open at a time.
    this.expandedCountriesSubject.next({ [key]: true });

    if (!this.selectedCountryMetricSubject.value[key]) {
      this.selectedCountryMetricSubject.next({
        ...this.selectedCountryMetricSubject.value,
        [key]: 'temperature'
      });
    }

    if (this.countryForecastsSubject.value[key]) {
      return;
    }

    this.setCountryLoading(key, true);
    this.store
      .getNextFiveDays(card.locationId)
      .pipe(
        finalize(() => this.setCountryLoading(key, false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (forecast) => {
          const barChart = this.buildCountryBarChart(forecast);
          const lineCharts = this.buildCountryLineCharts(forecast);
          this.countryForecastsSubject.next({
            ...this.countryForecastsSubject.value,
            [key]: { forecast, barChart, lineCharts }
          });
        },
        error: () => {
          this.setCountryLoading(key, false);
        }
      });
  }

  onCountryCardKeydown(event: KeyboardEvent, card: CountryCardViewModel): void {
    if (event.key !== 'Enter' && event.key !== ' ') {
      return;
    }

    event.preventDefault();
    this.toggleCountryCard(card);
  }

  selectCountryMetric(country: string, metric: CountryMetricKey, event: Event): void {
    event.stopPropagation();
    this.selectedCountryMetricSubject.next({
      ...this.selectedCountryMetricSubject.value,
      [country]: metric
    });
  }

  isMetricSelected(
    country: string,
    metric: CountryMetricKey,
    selectedMetrics: Partial<Record<string, CountryMetricKey>>
  ): boolean {
    return (selectedMetrics[country] ?? 'temperature') === metric;
  }

  getMetricButtonLabel(metric: CountryMetricKey): string {
    switch (metric) {
      case 'temperature':
        return 'Temperature';
      case 'humidity':
        return 'Humidity';
      case 'windSpeed':
        return 'Wind';
      default:
        return metric;
    }
  }

  trackByCountry(index: number, item: CountryCardViewModel): string {
    return item.country;
  }

  isAnyCountryExpanded(expandedCountries: Record<string, boolean>): boolean {
    return Object.values(expandedCountries).some((value) => value);
  }

  getVisibleCountryCards(
    cards: CountryCardViewModel[],
    expandedCountries: Record<string, boolean>
  ): CountryCardViewModel[] {
    const expandedCountry = Object.entries(expandedCountries).find((entry) => entry[1])?.[0];
    if (!expandedCountry) {
      return cards;
    }

    return cards.filter((card) => card.country === expandedCountry);
  }

  async exportSelectedCountryPdf(): Promise<void> {
    const expandedCountry = Object.entries(this.expandedCountriesSubject.value).find((entry) => entry[1])?.[0];
    if (!expandedCountry) {
      this.notifications.notify('Select a country card first, then export selected PDF.', 'warning', { showToast: true });
      return;
    }

    const card = this.getCountryCardsSnapshot().find((item) => item.country === expandedCountry && item.locationId);
    if (!card || !card.locationId) {
      this.notifications.notify('Unable to export selected country.', 'error', { showToast: true });
      return;
    }

    this.exportingPdfSubject.next(true);

    try {
      const forecast = await this.getForecastForCard(card);
      if (!forecast) {
        this.notifications.notify(`No forecast data found for ${expandedCountry}.`, 'warning', { showToast: true });
        return;
      }

      await this.pdfExport.exportCountryForecast(expandedCountry, forecast);
      this.notifications.notify(`Exported PDF for ${expandedCountry}.`, 'success', { showToast: true });
    } finally {
      this.exportingPdfSubject.next(false);
    }
  }

  async exportAllCountriesPdf(): Promise<void> {
    const cards = this.getCountryCardsSnapshot().filter((item) => item.locationId);
    if (cards.length === 0) {
      this.notifications.notify('No countries available to export.', 'warning', { showToast: true });
      return;
    }

    this.exportingPdfSubject.next(true);

    try {
      const results = await Promise.all(
        cards.map(async (card) => {
          const forecast = await this.getForecastForCard(card);
          return { country: card.country, forecast };
        })
      );

      const validResults = results
        .filter((result) => result.forecast)
        .map((result) => ({ country: result.country, forecast: result.forecast as NextFiveDayForecastDto }));

      if (validResults.length === 0) {
        this.notifications.notify('Unable to export. No forecast data available.', 'warning', { showToast: true });
        return;
      }

      await this.pdfExport.exportAllCountryForecasts(validResults);
      this.notifications.notify('Exported PDF for all countries.', 'success', { showToast: true });
    } finally {
      this.exportingPdfSubject.next(false);
    }
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

  private getCountryCardsSnapshot(): CountryCardViewModel[] {
    return this.buildCountryCards(this.currentLocations, this.currentWeatherByLocation);
  }

  private getCountrySet(locations: LocationDto[]): Set<string> {
    return new Set(locations.map((location) => (location.country?.trim().toUpperCase() || 'N/A').trim()));
  }

  private getCountryDifference(source: Set<string>, target: Set<string>): string[] {
    return [...source].filter((country) => !target.has(country));
  }

  private async getForecastForCard(card: CountryCardViewModel): Promise<NextFiveDayForecastDto | null> {
    const cached = this.countryForecastsSubject.value[card.country]?.forecast;
    if (cached) {
      return cached;
    }

    if (!card.locationId) {
      return null;
    }

    try {
      const forecast = await firstValueFrom(this.store.getNextFiveDays(card.locationId));
      const barChart = this.buildCountryBarChart(forecast);
      const lineCharts = this.buildCountryLineCharts(forecast);
      this.countryForecastsSubject.next({
        ...this.countryForecastsSubject.value,
        [card.country]: { forecast, barChart, lineCharts }
      });
      return forecast;
    } catch {
      return null;
    }
  }

  private buildCountryBarChart(forecast: NextFiveDayForecastDto): CountryClusteredBarChartViewModel {
    const width = 720;
    const height = 300;
    const padding = 48;

    if (forecast.days.length === 0) {
      return {
        width,
        height,
        padding,
        yAxis: [],
        groups: [],
        legend: [
          { label: `Temperature (${this.getTempUnit(forecast.units)})`, color: this.metricPalette.temperature },
          { label: 'Humidity (%)', color: this.metricPalette.humidity },
          { label: `Wind (${this.getWindUnit(forecast.units)})`, color: this.metricPalette.windSpeed }
        ]
      };
    }

    const metrics: CountryMetricKey[] = ['temperature', 'humidity', 'windSpeed'];
    const maxByMetric = metrics.reduce<Record<CountryMetricKey, number>>(
      (acc, metric) => {
        const values = forecast.days.map((day) => this.getMetricValue(day, metric));
        acc[metric] = Math.max(...values, 1);
        return acc;
      },
      { temperature: 1, humidity: 1, windSpeed: 1 }
    );

    const chartHeight = height - padding * 2;
    const chartWidth = width - padding * 2;
    const groupWidth = chartWidth / forecast.days.length;
    const barGap = Math.max(4, Math.min(10, groupWidth * 0.06));
    const barWidth = Math.max(12, Math.min(20, (groupWidth - barGap * 4) / 3));
    const clusterWidth = barWidth * 3 + barGap * 2;

    const groups = forecast.days.map((day, index) => {
      const centerX = padding + groupWidth * index + groupWidth / 2;
      const startX = centerX - clusterWidth / 2;
      const label = new Date(day.dateUtc).toLocaleDateString(undefined, { weekday: 'short' });

      const bars = metrics.map((metric, metricIndex) => {
        const value = this.getMetricValue(day, metric);
        const normalized = maxByMetric[metric] === 0 ? 0 : value / maxByMetric[metric];
        const barHeight = normalized * chartHeight;
        const x = startX + metricIndex * (barWidth + barGap);
        const y = height - padding - barHeight;

        return {
          metric,
          color: this.metricPalette[metric],
          valueLabel: this.formatMetricValue(metric, value, forecast.units),
          x,
          y,
          width: barWidth,
          height: barHeight
        };
      });

      return { label, labelX: centerX, bars };
    });

    const yAxis = Array.from({ length: 5 }, (_, index) => {
      const ratio = index / 4;
      const y = padding + ratio * chartHeight;
      const value = 100 - ratio * 100;
      return { y, label: `${Math.round(value)}%` };
    });

    return {
      width,
      height,
      padding,
      yAxis,
      groups,
      legend: [
        { label: `Temperature (${this.getTempUnit(forecast.units)})`, color: this.metricPalette.temperature },
        { label: 'Humidity (%)', color: this.metricPalette.humidity },
        { label: `Wind (${this.getWindUnit(forecast.units)})`, color: this.metricPalette.windSpeed }
      ]
    };
  }

  private buildCountryLineCharts(forecast: NextFiveDayForecastDto): Record<CountryMetricKey, SingleMetricLineChartViewModel> {
    return {
      temperature: this.buildSingleMetricLineChart(forecast, 'temperature'),
      humidity: this.buildSingleMetricLineChart(forecast, 'humidity'),
      windSpeed: this.buildSingleMetricLineChart(forecast, 'windSpeed')
    };
  }

  private buildSingleMetricLineChart(
    forecast: NextFiveDayForecastDto,
    metric: CountryMetricKey
  ): SingleMetricLineChartViewModel {
    const width = 680;
    const height = 250;
    const padding = 44;
    const color = this.metricPalette[metric];
    const label = this.getMetricLabel(metric, forecast.units);

    if (forecast.days.length === 0) {
      return {
        metric,
        label,
        axisTitle: label,
        color,
        width,
        height,
        padding,
        xAxis: [],
        yAxis: [],
        points: '',
        dots: []
      };
    }

    const labels = forecast.days.map((day) =>
      new Date(day.dateUtc).toLocaleDateString(undefined, { weekday: 'short' })
    );
    const xStep = labels.length > 1 ? (width - padding * 2) / (labels.length - 1) : 0;

    const values = forecast.days.map((day) => this.getMetricValue(day, metric));
    const minValue = Math.min(...values);
    const maxValue = Math.max(...values);
    const minBaseline = minValue - Math.max(1, (maxValue - minValue) * 0.12);
    const maxBaseline = maxValue + Math.max(1, (maxValue - minValue) * 0.12);
    const span = maxBaseline - minBaseline || 1;

    const toPoint = (value: number, index: number): { x: number; y: number } => {
      const x = labels.length > 1 ? padding + index * xStep : width / 2;
      const y = height - padding - ((value - minBaseline) / span) * (height - padding * 2);
      return { x, y };
    };

    const dots = values.map((value, index) => {
      const point = toPoint(value, index);
      return {
        ...point,
        valueLabel: this.formatMetricValue(metric, value, forecast.units)
      };
    });
    const points = dots.map((dot) => `${dot.x.toFixed(1)},${dot.y.toFixed(1)}`).join(' ');

    const yAxis = Array.from({ length: 5 }, (_, index) => {
      const ratio = index / 4;
      const y = padding + ratio * (height - padding * 2);
      const value = maxBaseline - ratio * span;
      return {
        y,
        label: this.formatMetricAxisValue(metric, value)
      };
    });

    const xAxis = labels.map((label, index) => {
      const x = labels.length > 1 ? padding + index * xStep : width / 2;
      return { label, x };
    });

    return {
      metric,
      label,
      axisTitle: label,
      color,
      width,
      height,
      padding,
      xAxis,
      yAxis,
      points,
      dots
    };
  }

  private getMetricValue(day: DailyWeatherPointDto, metric: CountryMetricKey): number {
    switch (metric) {
      case 'temperature':
        return day.temperature;
      case 'humidity':
        return day.humidity;
      case 'windSpeed':
        return day.windSpeed;
      default:
        return day.temperature;
    }
  }

  private getMetricLabel(metric: CountryMetricKey, units: TemperatureUnit): string {
    switch (metric) {
      case 'temperature':
        return `Temperature (${this.getTempUnit(units)})`;
      case 'humidity':
        return 'Humidity (%)';
      case 'windSpeed':
        return `Wind (${this.getWindUnit(units)})`;
      default:
        return metric;
    }
  }

  private formatMetricAxisValue(metric: CountryMetricKey, value: number): string {
    if (metric === 'humidity') {
      return `${Math.round(value)}`;
    }

    return value.toFixed(1);
  }

  private formatMetricValue(metric: CountryMetricKey, value: number, units: TemperatureUnit): string {
    switch (metric) {
      case 'temperature':
        return `${value.toFixed(1)}${this.getTempUnit(units)}`;
      case 'humidity':
        return `${Math.round(value)}%`;
      case 'windSpeed':
        return `${value.toFixed(1)} ${this.getWindUnit(units)}`;
      default:
        return value.toFixed(1);
    }
  }
}
