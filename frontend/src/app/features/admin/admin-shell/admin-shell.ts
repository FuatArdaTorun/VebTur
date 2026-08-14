import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { AdminReservationsService } from '../reservations/admin-reservations.service';
import { NotificationBell } from '../../../shared/notification-bell/notification-bell';
import { NotificationBellItem } from '../../../shared/notification-bell/notification-bell.model';

const PENDING_APPROVAL_LIMIT = 10;

@Component({
  selector: 'app-admin-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, NotificationBell],
  templateUrl: './admin-shell.html',
  styleUrl: './admin-shell.scss',
})
export class AdminShell {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly reservationsService = inject(AdminReservationsService);

  protected readonly currentUser = this.authService.currentUser;
  protected readonly pendingApprovals = signal<NotificationBellItem[]>([]);

  constructor() {
    this.fetchPendingApprovals();
  }

  protected logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  protected fetchPendingApprovals(): void {
    this.reservationsService
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
