import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminAmenity, AdminAmenityUpsert } from './models/admin-amenity.model';

@Injectable({ providedIn: 'root' })
export class AdminAmenitiesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/admin/amenities`;

  getAmenities() {
    return this.http.get<AdminAmenity[]>(this.baseUrl);
  }

  createAmenity(dto: AdminAmenityUpsert) {
    return this.http.post<AdminAmenity>(this.baseUrl, dto);
  }

  updateAmenity(id: string, dto: AdminAmenityUpsert) {
    return this.http.put<AdminAmenity>(`${this.baseUrl}/${id}`, dto);
  }

  deleteAmenity(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
