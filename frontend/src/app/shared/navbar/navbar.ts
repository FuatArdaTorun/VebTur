import { Component, ElementRef, HostListener, computed, effect, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ReservationsService } from '../../features/reservations/reservations.service';
import { AdminReservationsService } from '../../features/admin/reservations/admin-reservations.service';
import { NotificationBell } from '../notification-bell/notification-bell';
import { NotificationBellItem } from '../notification-bell/notification-bell.model';
import { ReservationStatus } from '../../features/reservations/models/reservation.model';

const DECIDED_STATUSES = new Set<ReservationStatus>(['Confirmed', 'Rejected', 'Cancelled']);
const RECENT_STATUS_CHANGES_LIMIT = 5;
const PENDING_APPROVAL_LIMIT = 10;

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, NotificationBell],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  private readonly authService = inject(AuthService);
  private readonly reservationsService = inject(ReservationsService);
  private readonly adminReservationsService = inject(AdminReservationsService);
  private readonly router = inject(Router);
  private readonly elementRef = inject(ElementRef);

  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly isAdmin = computed(() => this.authService.hasRole('Admin'));
  // First name reads friendlier in the greeting; falls back to the registered display name for
  // anyone who hasn't set a first name on their profile yet.
  protected readonly displayName = computed(() => {
    const user = this.authService.currentUser();
    return user?.firstName || user?.displayName || '';
  });
  protected readonly avatarInitial = computed(() => {
    const user = this.authService.currentUser();
    const source = user?.firstName || user?.displayName || user?.email || '?';
    return source.charAt(0).toUpperCase();
  });
  protected readonly recentStatusChanges = signal<NotificationBellItem[]>([]);
  protected readonly pendingApprovals = signal<NotificationBellItem[]>([]);
  protected readonly menuOpen = signal(false);
  protected readonly mobileMenuOpen = signal(false);

  constructor() {
    effect(() => {
      if (!this.isAuthenticated()) {
        return;
      }

      if (this.isAdmin()) {
        this.fetchPendingApprovals();
      } else {
        this.fetchRecentStatusChanges();
      }
    });
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      if (this.menuOpen()) this.menuOpen.set(false);
      if (this.mobileMenuOpen()) this.mobileMenuOpen.set(false);
    }
  }

  protected toggleMenu(): void {
    this.menuOpen.set(!this.menuOpen());
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected toggleMobileMenu(): void {
    this.mobileMenuOpen.set(!this.mobileMenuOpen());
  }

  protected closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  protected logout(): void {
    this.closeMenu();
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

  protected fetchPendingApprovals(): void {
    this.adminReservationsService
      .getReservations({ status: 'AwaitingApproval', sort: 'created-desc', page: 1, pageSize: PENDING_APPROVAL_LIMIT })
      .subscribe((result) => {
        this.pendingApprovals.set(
          result.items.map((r) => ({
            id: r.id,
            title: `${r.hotelName} — ${r.referenceNumber}`,
            subtitle: `${r.guestFullName} · awaiting approval`,
            routerLink: ['/admin/reservations', r.id],
          })),
        );
      });
  }
}
