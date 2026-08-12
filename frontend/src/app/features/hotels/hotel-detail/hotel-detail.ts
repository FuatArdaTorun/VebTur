import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HotelsService } from '../hotels.service';
import { HotelDetail as HotelDetailModel } from '../models/hotel.model';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../shared/error-state/error-state';

@Component({
  selector: 'app-hotel-detail',
  imports: [LoadingState, ErrorState, RouterLink, DecimalPipe],
  templateUrl: './hotel-detail.html',
  styleUrl: './hotel-detail.scss',
})
export class HotelDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly hotelsService = inject(HotelsService);

  protected readonly hotel = signal<HotelDetailModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    const idOrSlug = this.route.snapshot.paramMap.get('idOrSlug');
    if (!idOrSlug) {
      this.error.set(true);
      this.loading.set(false);
      return;
    }

    this.hotelsService.getHotel(idOrSlug).subscribe({
      next: (hotel) => {
        this.hotel.set(hotel);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  protected mapUrl(hotel: HotelDetailModel): string {
    // Search by name + address (not just coordinates) so Google Maps resolves directly to
    // the hotel's own business listing — with its photos, reviews, and info card — rather
    // than dropping a bare pin at a nearby coordinate.
    const query = encodeURIComponent(`${hotel.name}, ${hotel.address}, ${hotel.city}, ${hotel.country}`);
    return `https://www.google.com/maps/search/?api=1&query=${query}`;
  }
}
