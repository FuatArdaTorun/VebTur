import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { AdminNotificationsService } from '../admin-notifications.service';
import { AdminNotificationLog, NotificationStatus } from '../models/admin-notification.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { EmptyState } from '../../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../../shared/error-state/error-state';

const PAGE_SIZE = 20;
const STATUSES: NotificationStatus[] = ['Sent'];

@Component({
  selector: 'app-admin-notification-list',
  imports: [RouterLink, ReactiveFormsModule, DatePipe, LoadingState, EmptyState, ErrorState],
  templateUrl: './admin-notification-list.html',
  styleUrl: './admin-notification-list.scss',
})
export class AdminNotificationList {
  private readonly notificationsService = inject(AdminNotificationsService);

  protected readonly statuses = STATUSES;
  protected readonly notifications = signal<AdminNotificationLog[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(0);

  protected readonly statusControl = new FormControl<NotificationStatus | ''>('', { nonNullable: true });
  protected readonly searchControl = new FormControl('', { nonNullable: true });

  constructor() {
    this.fetch();
  }

  protected applyFilter(): void {
    this.page.set(1);
    this.fetch();
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }

    this.page.set(page);
    this.fetch();
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(false);

    this.notificationsService
      .getNotifications({
        status: this.statusControl.value || undefined,
        search: this.searchControl.value || undefined,
        page: this.page(),
        pageSize: PAGE_SIZE,
      })
      .subscribe({
        next: (result) => {
          this.notifications.set(result.items);
          this.totalPages.set(result.totalPages);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }
}
