import { Component, inject } from '@angular/core';
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

  protected readonly currentUser = this.authService.currentUser;

  protected logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/');
  }
}
