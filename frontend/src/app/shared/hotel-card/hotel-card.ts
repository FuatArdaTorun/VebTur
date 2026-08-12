import { Component, input } from '@angular/core';
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
}
