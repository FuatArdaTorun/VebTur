import { Component, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HotelSummary } from '../../features/hotels/models/hotel.model';

@Component({
  selector: 'app-hotel-card',
  imports: [RouterLink, DecimalPipe],
  templateUrl: './hotel-card.html',
  styleUrl: './hotel-card.scss',
})
export class HotelCard {
  readonly hotel = input.required<HotelSummary>();

  /** Heart toggle is opt-in — omitted entirely (not just disabled) when the caller has no
   * favorited-state to report, e.g. an unauthenticated visitor. */
  readonly showFavorite = input(false);
  readonly favorited = input(false);
  readonly favoriteToggle = output<void>();

  protected onFavoriteClick(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.favoriteToggle.emit();
  }
}
