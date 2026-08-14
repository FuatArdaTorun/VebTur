import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly isAdmin = computed(() => this.authService.hasRole('Admin'));
  protected readonly displayName = computed(() => this.authService.currentUser()?.displayName ?? '');

  protected logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/');
  }
}
