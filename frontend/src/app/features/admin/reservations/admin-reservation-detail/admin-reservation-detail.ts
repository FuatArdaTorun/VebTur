import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AdminReservationsService } from '../admin-reservations.service';
import { AdminReservationDetail as AdminReservationDetailModel } from '../models/admin-reservation.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../../shared/confirm-dialog/confirm-dialog';
import { StatusLabelPipe } from '../../../../shared/status-label/status-label.pipe';

@Component({
  selector: 'app-admin-reservation-detail',
  imports: [RouterLink, DatePipe, LoadingState, ErrorState, ConfirmDialog, StatusLabelPipe],
  templateUrl: './admin-reservation-detail.html',
  styleUrl: './admin-reservation-detail.scss',
})
export class AdminReservationDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly reservationsService = inject(AdminReservationsService);

  protected readonly reservation = signal<AdminReservationDetailModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly actionError = signal<string | null>(null);
  protected readonly pendingCancel = signal(false);

  constructor() {
    this.fetch();
  }

  protected canActOn(): boolean {
    return this.reservation()?.status === 'AwaitingApproval';
  }

  /**
   * Admin-side cancel only makes sense from Confirmed — that's the only status where it does
   * anything Reject doesn't (releasing the held room slot). A still-AwaitingApproval reservation
   * should be Rejected instead; showing both there was redundant (matches the backend guard in
   * AdminReservationService.CancelAsync).
   */
  protected canCancel(): boolean {
    return this.reservation()?.status === 'Confirmed';
  }

  protected confirm(): void {
    const reservation = this.reservation();
    if (!reservation) {
      return;
    }

    this.actionError.set(null);
    this.reservationsService.confirm(reservation.id).subscribe({
      next: () => this.fetch(),
      error: (response: HttpErrorResponse) => this.actionError.set(extractErrorMessage(response)),
    });
  }

  protected reject(): void {
    const reservation = this.reservation();
    if (!reservation) {
      return;
    }

    this.actionError.set(null);
    this.reservationsService.reject(reservation.id).subscribe({
      next: () => this.fetch(),
      error: (response: HttpErrorResponse) => this.actionError.set(extractErrorMessage(response)),
    });
  }

  protected confirmCancel(): void {
    const reservation = this.reservation();
    if (!reservation) {
      return;
    }

    this.actionError.set(null);
    this.reservationsService.cancel(reservation.id).subscribe({
      next: () => {
        this.pendingCancel.set(false);
        this.fetch();
      },
      error: (response: HttpErrorResponse) => {
        this.pendingCancel.set(false);
        this.actionError.set(extractErrorMessage(response));
      },
    });
  }

  private fetch(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loadError.set(true);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.loadError.set(false);

    this.reservationsService.getReservation(id).subscribe({
      next: (result) => {
        this.reservation.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
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
