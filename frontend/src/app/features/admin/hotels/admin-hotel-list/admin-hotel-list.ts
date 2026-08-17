import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { AdminHotelsService } from '../admin-hotels.service';
import { AdminHotelSummary } from '../models/admin-hotel.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { EmptyState } from '../../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../../shared/confirm-dialog/confirm-dialog';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-admin-hotel-list',
  imports: [RouterLink, ReactiveFormsModule, DatePipe, LoadingState, EmptyState, ErrorState, ConfirmDialog],
  templateUrl: './admin-hotel-list.html',
  styleUrl: './admin-hotel-list.scss',
})
export class AdminHotelList {
  private readonly hotelsService = inject(AdminHotelsService);

  protected readonly hotels = signal<AdminHotelSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly pendingToggle = signal<AdminHotelSummary | null>(null);

  protected readonly selectionMode = signal(false);
  protected readonly selectedIds = signal<Set<string>>(new Set());
  protected readonly confirmingBulkDelete = signal(false);

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly showInactiveControl = new FormControl(false, { nonNullable: true });

  protected readonly selectedCount = computed(() => this.selectedIds().size);
  protected readonly isAllSelected = computed(() => {
    const eligible = this.hotels().filter((h) => this.canDelete(h));
    return eligible.length > 0 && eligible.every((h) => this.selectedIds().has(h.id));
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

  protected requestToggle(hotel: AdminHotelSummary): void {
    this.pendingToggle.set(hotel);
  }

  protected confirmToggle(): void {
    const hotel = this.pendingToggle();
    if (!hotel) {
      return;
    }

    const request = hotel.isActive
      ? this.hotelsService.deactivateHotel(hotel.id)
      : this.hotelsService.reactivateHotel(hotel.id);

    request.subscribe(() => {
      this.pendingToggle.set(null);
      this.fetch();
    });
  }

  /**
   * A hotel with reservation history can't be permanently deleted (FK Restrict — see
   * AdminHotelService.DeleteHotelPermanentlyAsync) — no checkbox for it here, so the admin sees
   * upfront it isn't selectable instead of finding out only after attempting the delete.
   */
  protected canDelete(hotel: AdminHotelSummary): boolean {
    return !hotel.hasReservationHistory;
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

  /** Only deletable-eligible rows carry a checkbox, so "select all" only ever targets those. */
  protected toggleSelectAll(): void {
    const eligible = this.hotels().filter((h) => this.canDelete(h));
    this.selectedIds.set(this.isAllSelected() ? new Set() : new Set(eligible.map((h) => h.id)));
  }

  protected requestBulkDelete(): void {
    if (this.selectedCount() > 0) {
      this.confirmingBulkDelete.set(true);
    }
  }

  protected confirmBulkDelete(): void {
    this.hotelsService.deleteHotelsPermanently([...this.selectedIds()]).subscribe(() => {
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

    this.hotelsService
      .getHotels({
        search: this.searchControl.value || undefined,
        isActive: this.showInactiveControl.value ? undefined : true,
        page: this.page(),
        pageSize: PAGE_SIZE,
      })
      .subscribe({
        next: (result) => {
          this.hotels.set(result.items);
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
