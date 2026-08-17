import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Amenity, HotelDetail, HotelSearchParams, HotelSummary, PagedResult } from './models/hotel.model';

@Injectable({ providedIn: 'root' })
export class HotelsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1`;

  getHotels(params: HotelSearchParams) {
    let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);

    if (params.city) httpParams = httpParams.set('city', params.city);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.minPrice != null) httpParams = httpParams.set('minPrice', params.minPrice);
    if (params.maxPrice != null) httpParams = httpParams.set('maxPrice', params.maxPrice);
    if (params.minStarRating != null) httpParams = httpParams.set('minStarRating', params.minStarRating);
    if (params.minCapacity != null) httpParams = httpParams.set('minCapacity', params.minCapacity);
    if (params.sort) httpParams = httpParams.set('sort', params.sort);
    if (params.amenities?.length) {
      for (const slug of params.amenities) {
        httpParams = httpParams.append('amenities', slug);
      }
    }

    return this.http.get<PagedResult<HotelSummary>>(`${this.baseUrl}/hotels`, { params: httpParams });
  }

  getHotel(idOrSlug: string) {
    return this.http.get<HotelDetail>(`${this.baseUrl}/hotels/${idOrSlug}`);
  }

  getAmenities() {
    return this.http.get<Amenity[]>(`${this.baseUrl}/amenities`);
  }
}
