import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { ThemeMode } from '../models';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private static readonly StorageKey = 'weather-assessment.theme';

  private readonly modeSubject = new BehaviorSubject<ThemeMode>('normal');
  readonly mode$ = this.modeSubject.asObservable();

  constructor() {
    const stored = localStorage.getItem(ThemeService.StorageKey);
    const mode: ThemeMode = stored === 'color' ? 'color' : 'normal';
    this.applyMode(mode);
  }

  toggleMode(): void {
    const nextMode: ThemeMode = this.modeSubject.value === 'normal' ? 'color' : 'normal';
    this.applyMode(nextMode);
  }

  private applyMode(mode: ThemeMode): void {
    document.body.classList.remove('theme-normal', 'theme-color');
    document.body.classList.add(mode === 'normal' ? 'theme-normal' : 'theme-color');

    localStorage.setItem(ThemeService.StorageKey, mode);
    this.modeSubject.next(mode);
  }
}
