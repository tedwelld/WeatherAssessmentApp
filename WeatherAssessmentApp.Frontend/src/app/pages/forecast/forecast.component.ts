import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BehaviorSubject, finalize, map, switchMap, tap } from 'rxjs';
import { WeatherForecastDto } from '../../core/models';
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

  readonly loading$ = new BehaviorSubject<boolean>(true);
  readonly forecast$ = new BehaviorSubject<WeatherForecastDto | null>(null);
  readonly error$ = this.store.error$;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        tap(() => this.loading$.next(true)),
        takeUntilDestroyed(),
        map((params) => Number(params.get('id'))),
        switchMap((id) => this.store.getForecast(id)),
        finalize(() => this.loading$.next(false))
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
    return new Date(value).toLocaleString();
  }
}
