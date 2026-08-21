import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminDashboardService } from './admin-dashboard.service';
import { AdminDashboardSummary } from './models/admin-dashboard-summary.model';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../shared/error-state/error-state';

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink, LoadingState, ErrorState],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.scss',
})
export class AdminDashboard {
  private readonly dashboardService = inject(AdminDashboardService);

  protected readonly summary = signal<AdminDashboardSummary | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    this.dashboardService.getSummary().subscribe({
      next: (result) => {
        this.summary.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
