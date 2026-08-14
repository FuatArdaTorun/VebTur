import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ReservationsService } from '../reservations.service';
import { ReservationRequestDetail } from '../models/reservation.model';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';

const CANCELLABLE_STATUSES = new Set(['Pending', 'Sent', 'Confirmed']);
const AWAITING_DECISION_STATUSES = new Set(['Pending', 'Sent']);

@Component({
  selector: 'app-my-reservation-detail',
  imports: [RouterLink, DatePipe, LoadingState, ErrorState, ConfirmDialog],
  templateUrl: './my-reservation-detail.html',
  styleUrl: './my-reservation-detail.scss',
})
export class MyReservationDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly reservationsService = inject(ReservationsService);

  protected readonly reservation = signal<ReservationRequestDetail | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly cancelling = signal(false);
  protected readonly confirmingCancel = signal(false);

  constructor() {
    this.fetch();
  }

  protected canManage(): boolean {
    const status = this.reservation()?.status;
    return status !== undefined && CANCELLABLE_STATUSES.has(status);
  }

  /** The "not confirmed until approved" notice only makes sense while a decision is still pending. */
  protected isAwaitingDecision(): boolean {
    const status = this.reservation()?.status;
    return status !== undefined && AWAITING_DECISION_STATUSES.has(status);
  }

  protected confirmCancel(): void {
    const reservation = this.reservation();
    if (!reservation) {
      return;
    }

    this.cancelling.set(true);
    this.reservationsService.cancelMine(reservation.id).subscribe({
      next: () => {
        this.confirmingCancel.set(false);
        this.cancelling.set(false);
        this.fetch();
      },
      error: () => {
        this.cancelling.set(false);
      },
    });
  }

  private fetch(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set(true);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(false);

    this.reservationsService.getMineById(id).subscribe({
      next: (result) => {
        this.reservation.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
