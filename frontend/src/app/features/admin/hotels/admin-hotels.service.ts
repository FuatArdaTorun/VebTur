import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminHotelDetail, AdminHotelListParams, AdminHotelSummary, AdminHotelUpsert, PagedResult } from './models/admin-hotel.model';

@Injectable({ providedIn: 'root' })
export class AdminHotelsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/admin/hotels`;

  getHotels(params: AdminHotelListParams) {
    let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.isActive != null) httpParams = httpParams.set('isActive', params.isActive);

    return this.http.get<PagedResult<AdminHotelSummary>>(this.baseUrl, { params: httpParams });
  }

  getHotel(id: string) {
    return this.http.get<AdminHotelDetail>(`${this.baseUrl}/${id}`);
  }

  createHotel(dto: AdminHotelUpsert) {
    return this.http.post<AdminHotelDetail>(this.baseUrl, dto);
  }

  updateHotel(id: string, dto: AdminHotelUpsert) {
    return this.http.put<AdminHotelDetail>(`${this.baseUrl}/${id}`, dto);
  }

  deactivateHotel(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  reactivateHotel(id: string) {
    return this.http.post<void>(`${this.baseUrl}/${id}/reactivate`, {});
  }

  /** Irreversible — removes the hotel and all its owned data from the database. */
  deleteHotelPermanently(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}/permanent`);
  }
}
