import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HotelsService } from '../hotels/hotels.service';
import { HotelSummary } from '../hotels/models/hotel.model';
import { HotelCard } from '../../shared/hotel-card/hotel-card';
import { LoadingState } from '../../shared/loading-state/loading-state';
import { ErrorState } from '../../shared/error-state/error-state';

@Component({
  selector: 'app-home',
  imports: [RouterLink, HotelCard, LoadingState, ErrorState],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly hotelsService = inject(HotelsService);

  protected readonly featuredHotels = signal<HotelSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    this.hotelsService.getHotels({ page: 1, pageSize: 6, sort: 'star-desc' }).subscribe({
      next: (result) => {
        this.featuredHotels.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
