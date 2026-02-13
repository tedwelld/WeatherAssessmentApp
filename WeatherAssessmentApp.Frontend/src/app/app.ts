import { AsyncPipe, DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { NotificationCenterService } from './core/services/notification-center.service';
import { ThemeService } from './core/services/theme.service';
import { WeatherVisualService } from './core/services/weather-visual.service';
import { WorldWeatherMapComponent } from './shared/world-weather-map/world-weather-map.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, AsyncPipe, NgIf, NgFor, NgClass, DatePipe, WorldWeatherMapComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly weatherVisual = inject(WeatherVisualService);
  private readonly notifications = inject(NotificationCenterService);
  private readonly theme = inject(ThemeService);

  notificationsOpen = false;

  readonly visual$ = this.weatherVisual.state$;
  readonly mode$ = this.theme.mode$;
  readonly notifications$ = this.notifications.notifications$;
  readonly unreadCount$ = this.notifications.unreadCount$;
  readonly toasts$ = this.notifications.toasts$;
  readonly now = new Date();

  toggleTheme(): void {
    this.theme.toggleMode();
  }

  toggleNotificationsPanel(): void {
    this.notificationsOpen = !this.notificationsOpen;
    if (this.notificationsOpen) {
      this.notifications.markAllRead();
    }
  }

  clearNotifications(): void {
    this.notifications.clearAll();
  }

  dismissToast(id: number): void {
    this.notifications.dismissToast(id);
  }
}
