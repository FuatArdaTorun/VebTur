import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { AdminNotificationsService } from '../admin-notifications.service';
import { AdminNotificationLog, NotificationStatus } from '../models/admin-notification.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { EmptyState } from '../../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../../shared/confirm-dialog/confirm-dialog';

const PAGE_SIZE = 20;
const STATUSES: NotificationStatus[] = ['Sent'];

@Component({
  selector: 'app-admin-notification-list',
  imports: [RouterLink, ReactiveFormsModule, DatePipe, LoadingState, EmptyState, ErrorState, ConfirmDialog],
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
  protected readonly selectedIds = signal<Set<string>>(new Set());
  protected readonly confirmingBulkDelete = signal(false);

  protected readonly statusControl = new FormControl<NotificationStatus | ''>('', { nonNullable: true });
  protected readonly searchControl = new FormControl('', { nonNullable: true });

  protected readonly selectedCount = computed(() => this.selectedIds().size);
  protected readonly isAllSelected = computed(() => {
    const items = this.notifications();
    return items.length > 0 && items.every((n) => this.selectedIds().has(n.id));
  });

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

  protected isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  protected toggleSelect(id: string): void {
    const next = new Set(this.selectedIds());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.selectedIds.set(next);
  }

  protected toggleSelectAll(): void {
    this.selectedIds.set(this.isAllSelected() ? new Set() : new Set(this.notifications().map((n) => n.id)));
  }

  protected requestBulkDelete(): void {
    if (this.selectedCount() > 0) {
      this.confirmingBulkDelete.set(true);
    }
  }

  protected confirmBulkDelete(): void {
    this.notificationsService.deleteNotifications([...this.selectedIds()]).subscribe(() => {
      this.confirmingBulkDelete.set(false);
      this.selectedIds.set(new Set());
      this.fetch();
    });
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(false);
    this.selectedIds.set(new Set());

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
