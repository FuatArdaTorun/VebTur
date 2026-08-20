import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { HotelSummary, PagedResult } from '../hotels/models/hotel.model';

@Injectable({ providedIn: 'root' })
export class FavoritesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1`;

  getMine(page: number, pageSize: number) {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<HotelSummary>>(`${this.baseUrl}/favorites/mine`, { params });
  }

  /** Cheap id-only list — used to mark which hotel cards are already favorited. */
  getMineIds() {
    return this.http.get<string[]>(`${this.baseUrl}/favorites/mine/ids`);
  }

  addFavorite(hotelId: string) {
    return this.http.post<void>(`${this.baseUrl}/hotels/${hotelId}/favorite`, {});
  }

  removeFavorite(hotelId: string) {
    return this.http.delete<void>(`${this.baseUrl}/hotels/${hotelId}/favorite`);
  }
}
