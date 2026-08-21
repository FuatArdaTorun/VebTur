import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminDashboardSummary } from './models/admin-dashboard-summary.model';

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/admin/dashboard`;

  getSummary() {
    return this.http.get<AdminDashboardSummary>(this.baseUrl);
  }
}
