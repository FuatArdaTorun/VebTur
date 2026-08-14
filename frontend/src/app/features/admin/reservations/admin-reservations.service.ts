import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminReservationDetail, AdminReservationListParams, AdminReservationSummary, PagedResult } from './models/admin-reservation.model';

@Injectable({ providedIn: 'root' })
export class AdminReservationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/admin/reservation-requests`;

  getReservations(params: AdminReservationListParams) {
    let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (Array.isArray(params.status)) {
      for (const status of params.status) {
        httpParams = httpParams.append('status', status);
      }
    } else if (params.status) {
      httpParams = httpParams.set('status', params.status);
    }
    if (params.hotelId) httpParams = httpParams.set('hotelId', params.hotelId);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.sort) httpParams = httpParams.set('sort', params.sort);

    return this.http.get<PagedResult<AdminReservationSummary>>(this.baseUrl, { params: httpParams });
  }

  getReservation(id: string) {
    return this.http.get<AdminReservationDetail>(`${this.baseUrl}/${id}`);
  }

  confirm(id: string) {
    return this.http.post<void>(`${this.baseUrl}/${id}/confirm`, {});
  }

  reject(id: string) {
    return this.http.post<void>(`${this.baseUrl}/${id}/reject`, {});
  }

  cancel(id: string) {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, {});
  }

  /** Irreversible — removes the reservation request and its notification logs from the database. */
  deleteReservation(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
