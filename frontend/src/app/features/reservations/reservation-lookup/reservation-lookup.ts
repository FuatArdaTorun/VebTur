import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ReservationsService } from '../reservations.service';
import { ReservationRequestDetail } from '../models/reservation.model';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../shared/error-state/error-state';

const AWAITING_DECISION_STATUSES = new Set(['Pending', 'Sent']);

@Component({
  selector: 'app-reservation-lookup',
  imports: [ReactiveFormsModule, DatePipe, LoadingState, ErrorState],
  templateUrl: './reservation-lookup.html',
  styleUrl: './reservation-lookup.scss',
})
export class ReservationLookup {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly reservationsService = inject(ReservationsService);

  protected readonly loading = signal(false);
  protected readonly error = signal(false);
  protected readonly reservation = signal<ReservationRequestDetail | null>(null);

  protected readonly form = new FormGroup({
    reference: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    const reference = this.route.snapshot.paramMap.get('reference');
    if (reference) {
      this.form.patchValue({ reference });
      this.lookup(reference);
    }
  }

  /** The "not confirmed until approved" notice only makes sense while a decision is still pending. */
  protected isAwaitingDecision(): boolean {
    const status = this.reservation()?.status;
    return status !== undefined && AWAITING_DECISION_STATUSES.has(status);
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.router.navigate(['/reservations/lookup', this.form.controls.reference.value.trim()]);
  }

  private lookup(reference: string): void {
    this.loading.set(true);
    this.error.set(false);
    this.reservation.set(null);

    this.reservationsService.getByReference(reference).subscribe({
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
