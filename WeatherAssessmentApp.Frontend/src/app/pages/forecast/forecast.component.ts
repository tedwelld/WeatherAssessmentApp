import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BehaviorSubject, finalize, map, switchMap, tap } from 'rxjs';
import { ForecastItemDto, NextFiveDayForecastDto } from '../../core/models';
import { WeatherVisualService } from '../../core/services/weather-visual.service';
import { WeatherStoreService } from '../../core/store/weather-store.service';

@Component({
  selector: 'app-forecast-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './forecast.component.html',
  styleUrl: './forecast.component.scss'
})
export class ForecastComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly store = inject(WeatherStoreService);
  private readonly weatherVisual = inject(WeatherVisualService);

  readonly loading$ = new BehaviorSubject<boolean>(true);
  readonly forecast$ = new BehaviorSubject<NextFiveDayForecastDto | null>(null);
  readonly error$ = this.store.error$;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        tap(() => {
          this.loading$.next(true);
          this.forecast$.next(null);
        }),
        takeUntilDestroyed(),
        map((params) => Number(params.get('id'))),
        switchMap((id) =>
          this.store.getNextFiveDays(id).pipe(
            tap((forecast) =>
              this.weatherVisual.applyFromForecast(
                forecast.days.map<ForecastItemDto>((item) => ({
                  forecastAtUtc: item.dateUtc,
                  temperature: item.temperature,
                  feelsLike: item.feelsLike,
                  humidity: item.humidity,
                  summary: item.summary,
                  iconCode: item.iconCode,
                  windSpeed: item.windSpeed
                }))
              )
            ),
            finalize(() => this.loading$.next(false))
          )
        )
      )
      .subscribe({
        next: (forecast) => {
          this.forecast$.next(forecast);
        }
      });
  }

  getUnitSymbol(units: string): string {
    return units === 'Imperial' ? 'F' : 'C';
  }

  getWindUnit(units: string): string {
    return units === 'Imperial' ? 'mph' : 'm/s';
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleDateString(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    });
  }
}
