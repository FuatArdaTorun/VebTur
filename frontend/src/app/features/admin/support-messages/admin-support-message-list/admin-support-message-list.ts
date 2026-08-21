import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { AdminSupportMessagesService } from '../admin-support-messages.service';
import { AdminSupportMessage, AdminSupportMessageSort } from '../models/admin-support-message.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { EmptyState } from '../../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../../shared/confirm-dialog/confirm-dialog';

const PAGE_SIZE = 20;

type SortableColumn = 'sender' | 'subject' | 'received' | 'status';

@Component({
  selector: 'app-admin-support-message-list',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, LoadingState, EmptyState, ErrorState, ConfirmDialog],
  templateUrl: './admin-support-message-list.html',
  styleUrl: './admin-support-message-list.scss',
})
export class AdminSupportMessageList {
  private readonly messagesService = inject(AdminSupportMessagesService);

  protected readonly messages = signal<AdminSupportMessage[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly sort = signal<AdminSupportMessageSort>('received-desc');

  protected readonly selectionMode = signal(false);
  protected readonly selectedIds = signal<Set<string>>(new Set());
  protected readonly confirmingBulkDelete = signal(false);

  protected readonly searchControl = new FormControl('', { nonNullable: true });

  protected readonly selectedCount = computed(() => this.selectedIds().size);
  protected readonly isAllSelected = computed(() => {
    const items = this.messages();
    return items.length > 0 && items.every((m) => this.selectedIds().has(m.id));
  });

  constructor() {
    this.fetch();
  }

  protected applySearch(): void {
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

  /** Clicking a header toggles asc/desc if it's already the active column, otherwise starts ascending. */
  protected toggleSort(column: SortableColumn): void {
    const [activeColumn, activeDirection] = this.sort().split('-') as [string, string];
    const nextDirection = activeColumn === column && activeDirection === 'asc' ? 'desc' : 'asc';
    this.sort.set(`${column}-${nextDirection}` as AdminSupportMessageSort);
    this.page.set(1);
    this.fetch();
  }

  protected sortIndicator(column: SortableColumn): string {
    const [activeColumn, activeDirection] = this.sort().split('-') as [string, string];
    if (activeColumn !== column) {
      return '';
    }

    return activeDirection === 'asc' ? '▲' : '▼';
  }

  /** Toggling off drops any in-progress selection so re-entering selection mode starts fresh. */
  protected toggleSelectionMode(): void {
    this.selectionMode.set(!this.selectionMode());
    this.selectedIds.set(new Set());
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
    this.selectedIds.set(this.isAllSelected() ? new Set() : new Set(this.messages().map((m) => m.id)));
  }

  protected requestBulkDelete(): void {
    if (this.selectedCount() > 0) {
      this.confirmingBulkDelete.set(true);
    }
  }

  protected confirmBulkDelete(): void {
    this.messagesService.deleteMessages([...this.selectedIds()]).subscribe(() => {
      this.confirmingBulkDelete.set(false);
      this.selectionMode.set(false);
      this.selectedIds.set(new Set());
      this.fetch();
    });
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(false);
    this.selectedIds.set(new Set());

    this.messagesService
      .getMessages({
        search: this.searchControl.value || undefined,
        sort: this.sort(),
        page: this.page(),
        pageSize: PAGE_SIZE,
      })
      .subscribe({
        next: (result) => {
          this.messages.set(result.items);
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
