import { Routes } from '@angular/router';
import { Home } from './features/home/home';
import { HotelList } from './features/hotels/hotel-list/hotel-list';
import { HotelDetail } from './features/hotels/hotel-detail/hotel-detail';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'hotels', component: HotelList },
  { path: 'hotels/:idOrSlug', component: HotelDetail },
  { path: '**', redirectTo: '' },
];
