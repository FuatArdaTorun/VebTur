import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { AdminReservationsService } from '../admin-reservations.service';
import { AdminReservationSort, AdminReservationSummary } from '../models/admin-reservation.model';
import { ReservationStatus } from '../../../reservations/models/reservation.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { EmptyState } from '../../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../../shared/confirm-dialog/confirm-dialog';

const PAGE_SIZE = 20;
const STATUSES: ReservationStatus[] = ['Pending', 'Sent', 'Confirmed', 'Rejected', 'Cancelled'];

type SortableColumn = 'hotel' | 'checkin' | 'status';

@Component({
  selector: 'app-admin-reservation-list',
  imports: [RouterLink, ReactiveFormsModule, DatePipe, LoadingState, EmptyState, ErrorState, ConfirmDialog],
  templateUrl: './admin-reservation-list.html',
  styleUrl: './admin-reservation-list.scss',
})
export class AdminReservationList {
  private readonly reservationsService = inject(AdminReservationsService);

  protected readonly statuses = STATUSES;
  protected readonly reservations = signal<AdminReservationSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly sort = signal<AdminReservationSort>('created-desc');
  protected readonly pendingDelete = signal<AdminReservationSummary | null>(null);

  protected readonly statusControl = new FormControl<ReservationStatus | ''>('', { nonNullable: true });
  protected readonly searchControl = new FormControl('', { nonNullable: true });

  constructor() {
    this.fetch();
  }

  protected applyFilter(): void {
    this.page.set(1);
    this.fetch();
  }

  protected requestDelete(reservation: AdminReservationSummary): void {
    this.pendingDelete.set(reservation);
  }

  protected confirmDelete(): void {
    const reservation = this.pendingDelete();
    if (!reservation) {
      return;
    }

    this.reservationsService.deleteReservation(reservation.id).subscribe(() => {
      this.pendingDelete.set(null);
      this.fetch();
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
