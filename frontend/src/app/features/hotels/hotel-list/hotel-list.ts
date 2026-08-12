import { Component, inject, signal } from '@angular/core';
import { HotelsService } from '../hotels.service';
import { HotelSummary } from '../models/hotel.model';
import { HotelCard } from '../../../shared/hotel-card/hotel-card';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';

@Component({
  selector: 'app-hotel-list',
  imports: [HotelCard, LoadingState, EmptyState, ErrorState],
  templateUrl: './hotel-list.html',
  styleUrl: './hotel-list.scss',
})
export class HotelList {
  private readonly hotelsService = inject(HotelsService);

  protected readonly hotels = signal<HotelSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    this.hotelsService.getHotels(1, 24).subscribe({
      next: (result) => {
        this.hotels.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
