import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminReviewListParams, AdminReviewSummary, PagedResult } from './models/admin-review.model';

@Injectable({ providedIn: 'root' })
export class AdminReviewsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/admin/reviews`;

  getReviews(params: AdminReviewListParams) {
    let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.sort) httpParams = httpParams.set('sort', params.sort);

    return this.http.get<PagedResult<AdminReviewSummary>>(this.baseUrl, { params: httpParams });
  }

  /** Soft — excludes the review from the public hotel page and its average rating; reversible via unhideReview. */
  hideReview(id: string) {
    return this.http.post<void>(`${this.baseUrl}/${id}/hide`, {});
  }

  unhideReview(id: string) {
    return this.http.post<void>(`${this.baseUrl}/${id}/unhide`, {});
  }

  /** Irreversible. */
  deleteReview(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Irreversible bulk delete. Ids that no longer exist are silently ignored by the backend. */
  deleteReviews(ids: string[]) {
    return this.http.delete<void>(this.baseUrl, { body: { ids } });
  }
}
