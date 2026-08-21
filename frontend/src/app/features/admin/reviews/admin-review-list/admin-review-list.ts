import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { AdminReviewsService } from '../admin-reviews.service';
import { AdminReviewSort, AdminReviewSummary } from '../models/admin-review.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { EmptyState } from '../../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../../shared/confirm-dialog/confirm-dialog';

const PAGE_SIZE = 20;

type SortableColumn = 'hotel' | 'reviewer' | 'rating' | 'status';

@Component({
  selector: 'app-admin-review-list',
  imports: [ReactiveFormsModule, DatePipe, LoadingState, EmptyState, ErrorState, ConfirmDialog],
  templateUrl: './admin-review-list.html',
  styleUrl: './admin-review-list.scss',
})
export class AdminReviewList {
  private readonly reviewsService = inject(AdminReviewsService);

  protected readonly reviews = signal<AdminReviewSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly sort = signal<AdminReviewSort>('created-desc');

  protected readonly selectionMode = signal(false);
  protected readonly selectedIds = signal<Set<string>>(new Set());
  protected readonly confirmingBulkDelete = signal(false);

  protected readonly searchControl = new FormControl('', { nonNullable: true });

  protected readonly selectedCount = computed(() => this.selectedIds().size);
  protected readonly isAllSelected = computed(() => {
    const items = this.reviews();
    return items.length > 0 && items.every((r) => this.selectedIds().has(r.id));
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
    this.sort.set(`${column}-${nextDirection}` as AdminReviewSort);
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

  protected toggleHidden(review: AdminReviewSummary): void {
    const request = review.isHidden ? this.reviewsService.unhideReview(review.id) : this.reviewsService.hideReview(review.id);
    request.subscribe(() => this.fetch());
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
    this.selectedIds.set(this.isAllSelected() ? new Set() : new Set(this.reviews().map((r) => r.id)));
  }

  protected requestBulkDelete(): void {
    if (this.selectedCount() > 0) {
      this.confirmingBulkDelete.set(true);
    }
  }

  protected confirmBulkDelete(): void {
    this.reviewsService.deleteReviews([...this.selectedIds()]).subscribe(() => {
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

    this.reviewsService
      .getReviews({
        search: this.searchControl.value || undefined,
        sort: this.sort(),
        page: this.page(),
        pageSize: PAGE_SIZE,
      })
      .subscribe({
        next: (result) => {
          this.reviews.set(result.items);
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
