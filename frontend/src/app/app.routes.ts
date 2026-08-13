import { Routes } from '@angular/router';
import { Home } from './features/home/home';
import { HotelList } from './features/hotels/hotel-list/hotel-list';
import { HotelDetail } from './features/hotels/hotel-detail/hotel-detail';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'hotels', component: HotelList },
  { path: 'hotels/:idOrSlug', component: HotelDetail },
  {
    path: 'admin',
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/admin/login/admin-login').then((m) => m.AdminLogin),
      },
      {
        path: '',
        loadComponent: () => import('./features/admin/admin-shell/admin-shell').then((m) => m.AdminShell),
        canActivate: [authGuard],
        children: [
          { path: '', redirectTo: 'hotels', pathMatch: 'full' },
          {
            path: 'hotels',
            loadComponent: () =>
              import('./features/admin/hotels/admin-hotel-list/admin-hotel-list').then((m) => m.AdminHotelList),
          },
          {
            path: 'hotels/new',
            loadComponent: () =>
              import('./features/admin/hotels/admin-hotel-form/admin-hotel-form').then((m) => m.AdminHotelForm),
          },
          {
            path: 'hotels/:id/edit',
            loadComponent: () =>
              import('./features/admin/hotels/admin-hotel-form/admin-hotel-form').then((m) => m.AdminHotelForm),
          },
          {
            path: 'amenities',
            loadComponent: () =>
              import('./features/admin/amenities/admin-amenity-list/admin-amenity-list').then((m) => m.AdminAmenityList),
          },
        ],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
