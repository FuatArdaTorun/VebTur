import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Amenity, HotelDetail, HotelSummary, PagedResult } from './models/hotel.model';

@Injectable({ providedIn: 'root' })
export class HotelsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1`;

  getHotels(page = 1, pageSize = 12) {
    return this.http.get<PagedResult<HotelSummary>>(`${this.baseUrl}/hotels`, {
      params: { page, pageSize },
    });
  }

  getHotel(idOrSlug: string) {
    return this.http.get<HotelDetail>(`${this.baseUrl}/hotels/${idOrSlug}`);
  }

  getAmenities() {
    return this.http.get<Amenity[]>(`${this.baseUrl}/amenities`);
  }
}
