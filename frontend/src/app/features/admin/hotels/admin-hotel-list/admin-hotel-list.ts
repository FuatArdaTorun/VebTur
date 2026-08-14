import { Component, inject, signal } from '@angular/core';
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
  protected readonly pendingDelete = signal<AdminHotelSummary | null>(null);

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly showInactiveControl = new FormControl(false, { nonNullable: true });

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

  protected requestDelete(hotel: AdminHotelSummary): void {
    this.pendingDelete.set(hotel);
  }

  protected confirmDelete(): void {
    const hotel = this.pendingDelete();
    if (!hotel) {
      return;
    }

    this.hotelsService.deleteHotelPermanently(hotel.id).subscribe(() => {
      this.pendingDelete.set(null);
      this.fetch();
    });
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(false);

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
