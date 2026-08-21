import { Routes } from '@angular/router';
import { Home } from './features/home/home';
import { HotelList } from './features/hotels/hotel-list/hotel-list';
import { HotelDetail } from './features/hotels/hotel-detail/hotel-detail';
import { authGuard } from './core/auth/auth.guard';
import { authenticatedGuard } from './core/auth/authenticated.guard';

export const routes: Routes = [
  { path: '', component: Home, title: 'VebTur' },
  { path: 'hotels', component: HotelList, title: 'Browse Hotels | VebTur' },
  // HotelDetail sets its own, more specific title (the hotel's name) once it loads — this is
  // just the fallback shown before that fetch resolves.
  { path: 'hotels/:idOrSlug', component: HotelDetail, title: 'Hotel | VebTur' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
    title: 'Sign In | VebTur',
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
    title: 'Sign Up | VebTur',
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password').then((m) => m.ForgotPassword),
    title: 'Forgot Password | VebTur',
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password').then((m) => m.ResetPassword),
    title: 'Reset Password | VebTur',
  },
  {
    path: 'help',
    loadComponent: () => import('./features/help/contact-support/contact-support').then((m) => m.ContactSupport),
    title: 'Help & Support | VebTur',
  },
  {
    path: 'reservations/new',
    loadComponent: () => import('./features/reservations/reservation-form/reservation-form').then((m) => m.ReservationForm),
    title: 'New Reservation | VebTur',
  },
  {
    path: 'reservations/success/:reference',
    loadComponent: () =>
      import('./features/reservations/reservation-success/reservation-success').then((m) => m.ReservationSuccess),
    title: 'Reservation Sent | VebTur',
  },
  {
    path: 'reservations/lookup',
    loadComponent: () =>
      import('./features/reservations/reservation-lookup/reservation-lookup').then((m) => m.ReservationLookup),
    title: 'Find My Reservation | VebTur',
  },
  {
    path: 'reservations/lookup/:reference',
    loadComponent: () =>
      import('./features/reservations/reservation-lookup/reservation-lookup').then((m) => m.ReservationLookup),
    title: 'Find My Reservation | VebTur',
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
            title: 'Personal Info | VebTur',
          },
          {
            path: 'security',
            loadComponent: () => import('./features/profile/profile-security/profile-security').then((m) => m.ProfileSecurity),
            title: 'Account Security | VebTur',
          },
        ],
      },
      {
        path: 'favorites',
        loadComponent: () => import('./features/favorites/favorite-list/favorite-list').then((m) => m.FavoriteList),
        title: 'My Favorites | VebTur',
      },
      {
        path: 'my-reservations',
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./features/reservations/my-reservation-list/my-reservation-list').then((m) => m.MyReservationList),
            title: 'My Reservations | VebTur',
          },
          {
            path: ':id/edit',
            loadComponent: () => import('./features/reservations/reservation-form/reservation-form').then((m) => m.ReservationForm),
            title: 'Edit Reservation | VebTur',
          },
          {
            path: ':id',
            loadComponent: () =>
              import('./features/reservations/my-reservation-detail/my-reservation-detail').then((m) => m.MyReservationDetail),
            title: 'Reservation Details | VebTur',
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
          { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
          {
            path: 'dashboard',
            loadComponent: () => import('./features/admin/dashboard/admin-dashboard').then((m) => m.AdminDashboard),
            title: 'Dashboard | VebTur Admin',
          },
          {
            path: 'hotels',
            loadComponent: () =>
              import('./features/admin/hotels/admin-hotel-list/admin-hotel-list').then((m) => m.AdminHotelList),
            title: 'Hotels | VebTur Admin',
          },
          {
            path: 'hotels/new',
            loadComponent: () =>
              import('./features/admin/hotels/admin-hotel-form/admin-hotel-form').then((m) => m.AdminHotelForm),
            title: 'New Hotel | VebTur Admin',
          },
          {
            path: 'hotels/:id/edit',
            loadComponent: () =>
              import('./features/admin/hotels/admin-hotel-form/admin-hotel-form').then((m) => m.AdminHotelForm),
            title: 'Edit Hotel | VebTur Admin',
          },
          {
            path: 'reservations',
            loadComponent: () =>
              import('./features/admin/reservations/admin-reservation-list/admin-reservation-list').then((m) => m.AdminReservationList),
            title: 'Reservations | VebTur Admin',
          },
          {
            path: 'reservations/:id',
            loadComponent: () =>
              import('./features/admin/reservations/admin-reservation-detail/admin-reservation-detail').then(
                (m) => m.AdminReservationDetail,
              ),
            title: 'Reservation Details | VebTur Admin',
          },
          {
            path: 'amenities',
            loadComponent: () =>
              import('./features/admin/amenities/admin-amenity-list/admin-amenity-list').then((m) => m.AdminAmenityList),
            title: 'Amenities | VebTur Admin',
          },
          {
            path: 'notifications',
            loadComponent: () =>
              import('./features/admin/notifications/admin-notification-list/admin-notification-list').then(
                (m) => m.AdminNotificationList,
              ),
            title: 'Notifications | VebTur Admin',
          },
          {
            path: 'reviews',
            loadComponent: () =>
              import('./features/admin/reviews/admin-review-list/admin-review-list').then((m) => m.AdminReviewList),
            title: 'Reviews | VebTur Admin',
          },
          {
            path: 'support-messages',
            loadComponent: () =>
              import('./features/admin/support-messages/admin-support-message-list/admin-support-message-list').then(
                (m) => m.AdminSupportMessageList,
              ),
            title: 'Support Messages | VebTur Admin',
          },
          {
            path: 'support-messages/:id',
            loadComponent: () =>
              import('./features/admin/support-messages/admin-support-message-detail/admin-support-message-detail').then(
                (m) => m.AdminSupportMessageDetail,
              ),
            title: 'Support Message | VebTur Admin',
          },
        ],
      },
    ],
  },
  {
    // Keeps the mistyped/stale URL visible in the address bar (standard 404 behavior) rather
    // than redirecting it away to a generic path.
    path: '**',
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
    title: 'Page Not Found | VebTur',
  },
];
