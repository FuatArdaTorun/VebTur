import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReservationsService } from '../reservations.service';
import { ReservationRequestDetail } from '../models/reservation.model';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { EmptyState } from '../../../shared/empty-state/empty-state';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-my-reservation-list',
  imports: [RouterLink, DatePipe, LoadingState, ErrorState, EmptyState],
  templateUrl: './my-reservation-list.html',
  styleUrl: './my-reservation-list.scss',
})
export class MyReservationList {
  private readonly reservationsService = inject(ReservationsService);

  protected readonly reservations = signal<ReservationRequestDetail[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(0);

  constructor() {
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

    this.reservationsService.getMine(this.page(), PAGE_SIZE).subscribe({
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
