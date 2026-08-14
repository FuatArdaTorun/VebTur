import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  CreateReservationRequest,
  PagedResult,
  ReservationRequestDetail,
  UpdateReservationRequest,
} from './models/reservation.model';

@Injectable({ providedIn: 'root' })
export class ReservationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/reservation-requests`;

  create(dto: CreateReservationRequest) {
    return this.http.post<ReservationRequestDetail>(this.baseUrl, dto);
  }

  getByReference(reference: string) {
    return this.http.get<ReservationRequestDetail>(`${this.baseUrl}/${reference}`);
  }

  getMine(page: number, pageSize: number, sortByUpdated = false) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (sortByUpdated) params = params.set('sortByUpdated', true);
    return this.http.get<PagedResult<ReservationRequestDetail>>(`${this.baseUrl}/mine`, { params });
  }

  getMineById(id: string) {
    return this.http.get<ReservationRequestDetail>(`${this.baseUrl}/mine/${id}`);
  }

  updateMine(id: string, dto: UpdateReservationRequest) {
    return this.http.put<ReservationRequestDetail>(`${this.baseUrl}/mine/${id}`, dto);
  }

  cancelMine(id: string) {
    return this.http.post<void>(`${this.baseUrl}/mine/${id}/cancel`, {});
  }
}
