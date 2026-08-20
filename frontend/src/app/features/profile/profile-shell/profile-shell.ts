import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-profile-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './profile-shell.html',
  styleUrl: './profile-shell.scss',
})
export class ProfileShell {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly isAdmin = computed(() => this.authService.hasRole('Admin'));

  // Same firstName-over-displayName precedence the navbar already uses.
  protected readonly displayName = computed(() => {
    const user = this.authService.currentUser();
    return user?.firstName || user?.displayName || '';
  });

  protected logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/');
  }
}
