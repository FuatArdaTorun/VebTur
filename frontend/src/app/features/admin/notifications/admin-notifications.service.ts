import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminNotificationListParams, AdminNotificationLog, PagedResult } from './models/admin-notification.model';

@Injectable({ providedIn: 'root' })
export class AdminNotificationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/admin/notifications`;

  getNotifications(params: AdminNotificationListParams) {
    let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);

    return this.http.get<PagedResult<AdminNotificationLog>>(this.baseUrl, { params: httpParams });
  }

  /** Irreversible. Ids that no longer exist are silently ignored by the backend. */
  deleteNotifications(ids: string[]) {
    return this.http.delete<void>(this.baseUrl, { body: { ids } });
  }
}
