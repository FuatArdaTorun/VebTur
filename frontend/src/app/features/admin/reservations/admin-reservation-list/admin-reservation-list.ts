import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { AdminReservationsService } from '../admin-reservations.service';
import { AdminReservationSort, AdminReservationSummary } from '../models/admin-reservation.model';
import { ReservationStatus } from '../../../reservations/models/reservation.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { EmptyState } from '../../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../../shared/confirm-dialog/confirm-dialog';
import { StatusLabelPipe } from '../../../../shared/status-label/status-label.pipe';
import { WarningBanner } from '../../../../shared/warning-banner/warning-banner';

const PAGE_SIZE = 20;
const STATUSES: ReservationStatus[] = ['AwaitingApproval', 'Confirmed', 'Rejected', 'Cancelled'];

type SortableColumn = 'hotel' | 'checkin' | 'status';

@Component({
  selector: 'app-admin-reservation-list',
  imports: [RouterLink, ReactiveFormsModule, DatePipe, LoadingState, EmptyState, ErrorState, ConfirmDialog, StatusLabelPipe, WarningBanner],
  templateUrl: './admin-reservation-list.html',
  styleUrl: './admin-reservation-list.scss',
})
export class AdminReservationList {
  private readonly reservationsService = inject(AdminReservationsService);
  private readonly route = inject(ActivatedRoute);

  protected readonly statuses = STATUSES;
  protected readonly reservations = signal<AdminReservationSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(0);
  // Status-ascending puts AwaitingApproval (ordinal 1, the actionable one) at the top by default,
  // so the admin sees pending requests first without having to click the Status column header.
  protected readonly sort = signal<AdminReservationSort>('status-asc');
  protected readonly deleteError = signal<string | null>(null);

  protected readonly selectionMode = signal(false);
  protected readonly selectedIds = signal<Set<string>>(new Set());
  protected readonly confirmingBulkDelete = signal(false);

  protected readonly statusControl = new FormControl<ReservationStatus | ''>('', { nonNullable: true });
  protected readonly searchControl = new FormControl('', { nonNullable: true });

  protected readonly selectedCount = computed(() => this.selectedIds().size);
  protected readonly isAllSelected = computed(() => {
    const eligible = this.reservations().filter((r) => this.canDelete(r));
    return eligible.length > 0 && eligible.every((r) => this.selectedIds().has(r.id));
  });

  constructor() {
    // Arriving via a link from elsewhere (e.g. "View this hotel's reservations" on a blocked
    // hotel delete) can pre-fill the search box — same filter mechanism as typing it in by hand,
    // so clearing/editing it clears the filter too, with nothing extra to learn or reset.
    const search = this.route.snapshot.queryParamMap.get('search');
    if (search) {
      this.searchControl.setValue(search);
    }
    this.fetch();
  }

  protected applyFilter(): void {
    this.page.set(1);
    this.fetch();
  }

  /**
   * A still-AwaitingApproval reservation must be Confirmed or Rejected first — deleting it
   * outright would silently discard a guest's request with no record it was ever acted on
   * (matches the backend guard in AdminReservationService.DeleteAsync).
   */
  protected canDelete(reservation: AdminReservationSummary): boolean {
    return reservation.status !== 'AwaitingApproval';
  }

  /** Toggling off drops any in-progress selection so re-entering selection mode starts fresh. */
  protected toggleSelectionMode(): void {
    this.selectionMode.set(!this.selectionMode());
    this.selectedIds.set(new Set());
    this.deleteError.set(null);
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

  /** Only AwaitingApproval-eligible rows carry a checkbox, so "select all" only ever targets those. */
  protected toggleSelectAll(): void {
    const eligible = this.reservations().filter((r) => this.canDelete(r));
    this.selectedIds.set(this.isAllSelected() ? new Set() : new Set(eligible.map((r) => r.id)));
  }

  protected requestBulkDelete(): void {
    if (this.selectedCount() > 0) {
      this.confirmingBulkDelete.set(true);
    }
  }

  protected confirmBulkDelete(): void {
    this.reservationsService.deleteReservations([...this.selectedIds()]).subscribe({
      next: () => {
        this.confirmingBulkDelete.set(false);
        this.selectionMode.set(false);
        this.selectedIds.set(new Set());
        this.fetch();
      },
      error: (response: HttpErrorResponse) => {
        this.confirmingBulkDelete.set(false);
        this.deleteError.set(extractErrorMessage(response));
      },
    });
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
    this.sort.set(`${column}-${nextDirection}` as AdminReservationSort);
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

  private fetch(): void {
    this.loading.set(true);
    this.error.set(false);
    this.selectedIds.set(new Set());

    this.reservationsService
      .getReservations({
        status: this.statusControl.value || undefined,
        search: this.searchControl.value || undefined,
        sort: this.sort(),
        page: this.page(),
        pageSize: PAGE_SIZE,
      })
      .subscribe({
        next: (result) => {
          this.reservations.set(result.items);
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

function extractErrorMessage(response: HttpErrorResponse): string {
  const errors = response.error?.errors;
  if (errors && typeof errors === 'object') {
    const firstMessage = Object.values(errors).flat()[0];
    if (typeof firstMessage === 'string') {
      return firstMessage;
    }
  }

  return 'This action could not be completed.';
}
