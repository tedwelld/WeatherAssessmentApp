import { Injectable } from '@angular/core';
import { BehaviorSubject, map } from 'rxjs';
import { AppNotification, AppNotificationType, AppToastMessage } from '../models';

@Injectable({ providedIn: 'root' })
export class NotificationCenterService {
  private readonly notificationsSubject = new BehaviorSubject<AppNotification[]>([]);
  private readonly toastsSubject = new BehaviorSubject<AppToastMessage[]>([]);
  private nextId = 1;

  readonly notifications$ = this.notificationsSubject.asObservable();
  readonly toasts$ = this.toastsSubject.asObservable();
  readonly unreadCount$ = this.notifications$.pipe(map((items) => items.filter((item) => item.unread).length));

  notify(
    message: string,
    type: AppNotificationType = 'info',
    options?: { showToast?: boolean; toastDurationMs?: number }
  ): void {
    const id = this.nextId++;
    const entry: AppNotification = {
      id,
      message,
      type,
      createdAtUtc: new Date().toISOString(),
      unread: true
    };

    this.notificationsSubject.next([entry, ...this.notificationsSubject.value].slice(0, 100));

    if (options?.showToast ?? false) {
      const toast: AppToastMessage = { id, message, type };
      this.toastsSubject.next([...this.toastsSubject.value, toast]);

      const duration = options?.toastDurationMs ?? 3500;
      setTimeout(() => this.dismissToast(id), duration);
    }
  }

  markAllRead(): void {
    this.notificationsSubject.next(this.notificationsSubject.value.map((item) => ({ ...item, unread: false })));
  }

  clearAll(): void {
    this.notificationsSubject.next([]);
  }

  dismissToast(id: number): void {
    this.toastsSubject.next(this.toastsSubject.value.filter((item) => item.id !== id));
  }
}
