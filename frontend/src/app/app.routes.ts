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
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password').then((m) => m.ForgotPassword),
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password').then((m) => m.ResetPassword),
  },
  {
    path: 'help',
    loadComponent: () => import('./features/help/contact-support/contact-support').then((m) => m.ContactSupport),
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
    // Wraps profile/favorites/my-reservations in one persistent account sidebar (mirrors
    // AdminShell) without changing any of their URLs — this path contributes no URL segment.
    path: '',
    canActivate: [authenticatedGuard],
    loadComponent: () => import('./features/profile/profile-shell/profile-shell').then((m) => m.ProfileShell),
    children: [
      {
        path: 'profile',
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
        loadComponent: () => import('./features/favorites/favorite-list/favorite-list').then((m) => m.FavoriteList),
      },
      {
        path: 'my-reservations',
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
          {
            path: 'support-messages',
            loadComponent: () =>
              import('./features/admin/support-messages/admin-support-message-list/admin-support-message-list').then(
                (m) => m.AdminSupportMessageList,
              ),
          },
          {
            path: 'support-messages/:id',
            loadComponent: () =>
              import('./features/admin/support-messages/admin-support-message-detail/admin-support-message-detail').then(
                (m) => m.AdminSupportMessageDetail,
              ),
          },
        ],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
