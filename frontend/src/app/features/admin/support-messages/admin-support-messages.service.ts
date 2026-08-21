import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminSupportMessage, AdminSupportMessageListParams, PagedResult } from './models/admin-support-message.model';

@Injectable({ providedIn: 'root' })
export class AdminSupportMessagesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/admin/support-messages`;

  getMessages(params: AdminSupportMessageListParams) {
    let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.sort) httpParams = httpParams.set('sort', params.sort);

    return this.http.get<PagedResult<AdminSupportMessage>>(this.baseUrl, { params: httpParams });
  }

  getMessage(id: string) {
    return this.http.get<AdminSupportMessage>(`${this.baseUrl}/${id}`);
  }

  /** Overwrites any previous reply — one current answer, not a threaded conversation. */
  reply(id: string, replyMessage: string) {
    return this.http.post<void>(`${this.baseUrl}/${id}/reply`, { replyMessage });
  }

  /** Irreversible. */
  deleteMessage(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Irreversible bulk delete. Ids that no longer exist are silently ignored by the backend. */
  deleteMessages(ids: string[]) {
    return this.http.delete<void>(this.baseUrl, { body: { ids } });
  }
}
