import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CreateSupportMessageRequest, SupportMessageConfirmation } from './models/support-message.model';

@Injectable({ providedIn: 'root' })
export class HelpService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1`;

  sendMessage(dto: CreateSupportMessageRequest) {
    return this.http.post<SupportMessageConfirmation>(`${this.baseUrl}/support-messages`, dto);
  }
}
