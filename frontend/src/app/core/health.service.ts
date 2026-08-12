import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface HealthStatus {
  status: string;
  databaseReachable: boolean;
  timestampUtc: string;
}

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);

  getHealth() {
    return this.http.get<HealthStatus>(`${environment.apiBaseUrl}/api/v1/health`);
  }
}
