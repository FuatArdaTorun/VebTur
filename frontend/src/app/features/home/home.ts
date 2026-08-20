import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HotelsService } from '../hotels/hotels.service';
import { HotelSummary } from '../hotels/models/hotel.model';
import { HotelCard } from '../../shared/hotel-card/hotel-card';
import { LoadingState } from '../../shared/loading-state/loading-state';
import { ErrorState } from '../../shared/error-state/error-state';
import { AuthService } from '../../core/auth/auth.service';
import { FavoritesService } from '../favorites/favorites.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink, HotelCard, LoadingState, ErrorState],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly hotelsService = inject(HotelsService);
  private readonly favoritesService = inject(FavoritesService);
  protected readonly authService = inject(AuthService);

  protected readonly featuredHotels = signal<HotelSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly favoriteIds = signal<Set<string>>(new Set());

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

    if (this.authService.isAuthenticated()) {
      this.favoritesService.getMineIds().subscribe((ids) => this.favoriteIds.set(new Set(ids)));
    }
  }

  protected toggleFavorite(hotelId: string): void {
    const isFavorited = this.favoriteIds().has(hotelId);
    const request = isFavorited ? this.favoritesService.removeFavorite(hotelId) : this.favoritesService.addFavorite(hotelId);

    request.subscribe(() => {
      this.favoriteIds.update((current) => {
        const next = new Set(current);
        isFavorited ? next.delete(hotelId) : next.add(hotelId);
        return next;
      });
    });
  }
}
