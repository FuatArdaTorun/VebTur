import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ReservationsService } from '../../features/reservations/reservations.service';
import { NotificationBell } from '../notification-bell/notification-bell';
import { NotificationBellItem } from '../notification-bell/notification-bell.model';
import { ReservationStatus } from '../../features/reservations/models/reservation.model';

const DECIDED_STATUSES = new Set<ReservationStatus>(['Confirmed', 'Rejected', 'Cancelled']);
const RECENT_STATUS_CHANGES_LIMIT = 5;

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, NotificationBell],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  private readonly authService = inject(AuthService);
  private readonly reservationsService = inject(ReservationsService);
  private readonly router = inject(Router);

  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly isAdmin = computed(() => this.authService.hasRole('Admin'));
  // First name reads friendlier in the greeting; falls back to the registered display name for
  // anyone who hasn't set a first name on their profile yet.
  protected readonly displayName = computed(() => {
    const user = this.authService.currentUser();
    return user?.firstName || user?.displayName || '';
  });
  protected readonly recentStatusChanges = signal<NotificationBellItem[]>([]);

  constructor() {
    // Customers only — admins get their own "pending approvals" bell in the admin shell instead.
    effect(() => {
      if (this.isAuthenticated() && !this.isAdmin()) {
        this.fetchRecentStatusChanges();
      }
    });
  }

  protected logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/');
  }

  protected fetchRecentStatusChanges(): void {
    this.reservationsService.getMine(1, 20, true).subscribe((result) => {
      this.recentStatusChanges.set(
        result.items
          .filter((r) => DECIDED_STATUSES.has(r.status))
          .slice(0, RECENT_STATUS_CHANGES_LIMIT)
          .map((r) => ({
            id: r.id,
            title: `${r.hotelName} — ${r.status}`,
            subtitle: `Reservation ${r.referenceNumber}: see details`,
            routerLink: ['/my-reservations', r.id],
          })),
      );
    });
  }
}
