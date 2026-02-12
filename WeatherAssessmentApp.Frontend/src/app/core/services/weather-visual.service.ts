import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { ForecastItemDto } from '../models';

export type WeatherVisualMode = 'clear' | 'clouds' | 'rain' | 'storm' | 'snow' | 'mist';

export interface WeatherVisualState {
  mode: WeatherVisualMode;
  rainOpacity: string;
  rainDuration: string;
  cloudOpacity: string;
  windOpacity: string;
  windDuration: string;
  auroraOpacity: string;
  lightningOpacity: string;
}

const DEFAULT_STATE: WeatherVisualState = {
  mode: 'clear',
  rainOpacity: '0.18',
  rainDuration: '1.7s',
  cloudOpacity: '0.55',
  windOpacity: '0.42',
  windDuration: '8s',
  auroraOpacity: '0.42',
  lightningOpacity: '0'
};

@Injectable({ providedIn: 'root' })
export class WeatherVisualService {
  private readonly stateSubject = new BehaviorSubject<WeatherVisualState>(DEFAULT_STATE);
  readonly state$ = this.stateSubject.asObservable();

  reset(): void {
    this.stateSubject.next(DEFAULT_STATE);
  }

  applyFromCurrent(summary: string, temperature: number, windSpeed: number, humidity: number): void {
    const mode = this.detectMode(summary);
    this.stateSubject.next(this.buildState(mode, windSpeed, humidity, temperature));
  }

  applyFromForecast(items: ForecastItemDto[]): void {
    if (!items.length) {
      this.reset();
      return;
    }

    const firstBucket = items.slice(0, 8);
    const dominant = firstBucket.reduce<Record<WeatherVisualMode, number>>(
      (acc, item) => {
        const mode = this.detectMode(item.summary);
        acc[mode] += 1;
        return acc;
      },
      {
        clear: 0,
        clouds: 0,
        rain: 0,
        storm: 0,
        snow: 0,
        mist: 0
      }
    );

    const mode = (Object.entries(dominant).sort((a, b) => b[1] - a[1])[0]?.[0] as WeatherVisualMode) ?? 'clear';
    const averageWind = firstBucket.reduce((sum, item) => sum + item.windSpeed, 0) / firstBucket.length;
    const averageHumidity = firstBucket.reduce((sum, item) => sum + item.humidity, 0) / firstBucket.length;
    const averageTemperature = firstBucket.reduce((sum, item) => sum + item.temperature, 0) / firstBucket.length;

    this.stateSubject.next(this.buildState(mode, averageWind, averageHumidity, averageTemperature));
  }

  private detectMode(summary: string): WeatherVisualMode {
    const text = summary.toLowerCase();

    if (text.includes('thunder') || text.includes('lightning') || text.includes('storm')) {
      return 'storm';
    }

    if (text.includes('snow') || text.includes('blizzard') || text.includes('sleet')) {
      return 'snow';
    }

    if (text.includes('rain') || text.includes('drizzle') || text.includes('shower')) {
      return 'rain';
    }

    if (text.includes('mist') || text.includes('fog') || text.includes('haze') || text.includes('smoke') || text.includes('dust')) {
      return 'mist';
    }

    if (text.includes('cloud') || text.includes('overcast')) {
      return 'clouds';
    }

    return 'clear';
  }

  private buildState(mode: WeatherVisualMode, windSpeed: number, humidity: number, temperature: number): WeatherVisualState {
    const clampedHumidity = this.clamp(humidity / 100, 0.15, 1);
    const normalizedWind = this.clamp(windSpeed / 12, 0.1, 1.2);

    const rainBase = mode === 'storm' ? 0.62 : mode === 'rain' ? 0.45 : mode === 'snow' ? 0.2 : 0.08;
    const cloudBase = mode === 'clear' ? 0.38 : mode === 'clouds' ? 0.74 : mode === 'mist' ? 0.8 : 0.68;
    const auroraBase = mode === 'storm' ? 0.25 : mode === 'clear' ? 0.48 : 0.36;

    const rainOpacity = this.clamp(rainBase * clampedHumidity, 0.04, 0.82);
    const rainDurationSeconds = this.clamp(2.1 - normalizedWind * 0.9, 0.75, 2.4);
    const windDurationSeconds = this.clamp(10.5 - normalizedWind * 4.5, 4.2, 11.5);

    const warmBoost = temperature > 30 ? 0.08 : temperature < 5 ? -0.05 : 0;

    return {
      mode,
      rainOpacity: rainOpacity.toFixed(2),
      rainDuration: `${rainDurationSeconds.toFixed(2)}s`,
      cloudOpacity: this.clamp(cloudBase + warmBoost, 0.2, 0.9).toFixed(2),
      windOpacity: this.clamp(0.3 + normalizedWind * 0.42, 0.16, 0.84).toFixed(2),
      windDuration: `${windDurationSeconds.toFixed(2)}s`,
      auroraOpacity: this.clamp(auroraBase + clampedHumidity * 0.12, 0.18, 0.62).toFixed(2),
      lightningOpacity: mode === 'storm' ? '0.55' : '0'
    };
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
  }
}
