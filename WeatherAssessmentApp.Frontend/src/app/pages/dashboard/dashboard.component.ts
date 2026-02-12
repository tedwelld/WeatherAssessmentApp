import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { combineLatest, map, startWith } from 'rxjs';
import { CurrentWeatherDto, LocationDto, TemperatureUnit, UserPreferencesDto } from '../../core/models';
import { WeatherStoreService } from '../../core/store/weather-store.service';

interface DashboardViewModel {
  locations: LocationDto[];
  weatherByLocation: Record<number, CurrentWeatherDto>;
  preferences: UserPreferencesDto | null;
  loading: boolean;
  error: string | null;
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
    error: this.store.error$
  }).pipe(
    map(({ locations, weather, preferences, loading, error }) => {
      const weatherByLocation = weather
        .filter((item) => item.locationId !== null)
        .reduce<Record<number, CurrentWeatherDto>>((acc, item) => {
          acc[item.locationId as number] = item;
          return acc;
        }, {});

      return {
        locations,
        weatherByLocation,
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
}
