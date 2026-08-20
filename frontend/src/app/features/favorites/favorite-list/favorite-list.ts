import { Component, inject, signal } from '@angular/core';
import { FavoritesService } from '../favorites.service';
import { HotelSummary } from '../../hotels/models/hotel.model';
import { HotelCard } from '../../../shared/hotel-card/hotel-card';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { EmptyState } from '../../../shared/empty-state/empty-state';

const PAGE_SIZE = 12;

@Component({
  selector: 'app-favorite-list',
  imports: [HotelCard, LoadingState, ErrorState, EmptyState],
  templateUrl: './favorite-list.html',
  styleUrl: './favorite-list.scss',
})
export class FavoriteList {
  private readonly favoritesService = inject(FavoritesService);

  protected readonly hotels = signal<HotelSummary[]>([]);
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

  protected removeFavorite(hotelId: string): void {
    this.favoritesService.removeFavorite(hotelId).subscribe(() => {
      this.hotels.update((list) => list.filter((h) => h.id !== hotelId));
    });
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(false);

    this.favoritesService.getMine(this.page(), PAGE_SIZE).subscribe({
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
