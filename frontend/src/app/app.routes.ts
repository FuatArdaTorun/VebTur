import { Routes } from '@angular/router';
import { Home } from './features/home/home';
import { HotelList } from './features/hotels/hotel-list/hotel-list';
import { HotelDetail } from './features/hotels/hotel-detail/hotel-detail';
import { authGuard } from './core/auth/auth.guard';
import { authenticatedGuard } from './core/auth/authenticated.guard';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'hotels', component: HotelList },
  { path: 'hotels/:idOrSlug', component: HotelDetail },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },
  {
    path: 'reservations/new',
    loadComponent: () => import('./features/reservations/reservation-form/reservation-form').then((m) => m.ReservationForm),
  },
  {
    path: 'reservations/success/:reference',
    loadComponent: () =>
      import('./features/reservations/reservation-success/reservation-success').then((m) => m.ReservationSuccess),
  },
  {
    path: 'reservations/lookup',
    loadComponent: () =>
      import('./features/reservations/reservation-lookup/reservation-lookup').then((m) => m.ReservationLookup),
  },
  {
    path: 'reservations/lookup/:reference',
    loadComponent: () =>
      import('./features/reservations/reservation-lookup/reservation-lookup').then((m) => m.ReservationLookup),
  },
  {
    path: 'profile',
    canActivate: [authenticatedGuard],
    loadComponent: () => import('./features/profile/profile-shell/profile-shell').then((m) => m.ProfileShell),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/profile/profile-info/profile-info').then((m) => m.ProfileInfo),
      },
      {
        path: 'security',
        loadComponent: () => import('./features/profile/profile-security/profile-security').then((m) => m.ProfileSecurity),
      },
    ],
  },
  {
    path: 'favorites',
    canActivate: [authenticatedGuard],
    loadComponent: () => import('./features/favorites/favorite-list/favorite-list').then((m) => m.FavoriteList),
  },
  {
    path: 'my-reservations',
    canActivate: [authenticatedGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/reservations/my-reservation-list/my-reservation-list').then((m) => m.MyReservationList),
      },
      {
        path: ':id/edit',
        loadComponent: () => import('./features/reservations/reservation-form/reservation-form').then((m) => m.ReservationForm),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./features/reservations/my-reservation-detail/my-reservation-detail').then((m) => m.MyReservationDetail),
      },
    ],
  },
  {
    path: 'admin',
    children: [
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
            path: 'reservations',
            loadComponent: () =>
              import('./features/admin/reservations/admin-reservation-list/admin-reservation-list').then((m) => m.AdminReservationList),
          },
          {
            path: 'reservations/:id',
            loadComponent: () =>
              import('./features/admin/reservations/admin-reservation-detail/admin-reservation-detail').then(
                (m) => m.AdminReservationDetail,
              ),
          },
          {
            path: 'amenities',
            loadComponent: () =>
              import('./features/admin/amenities/admin-amenity-list/admin-amenity-list').then((m) => m.AdminAmenityList),
          },
          {
            path: 'notifications',
            loadComponent: () =>
              import('./features/admin/notifications/admin-notification-list/admin-notification-list').then(
                (m) => m.AdminNotificationList,
              ),
          },
          {
            path: 'reviews',
            loadComponent: () =>
              import('./features/admin/reviews/admin-review-list/admin-review-list').then((m) => m.AdminReviewList),
          },
        ],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
