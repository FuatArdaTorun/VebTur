import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdminSupportMessagesService } from './admin-support-messages.service';

describe('AdminSupportMessagesService', () => {
  let service: AdminSupportMessagesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminSupportMessagesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getMessages GETs /admin/support-messages with search when provided', () => {
    service.getMessages({ search: 'refund', page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/support-messages'));
    expect(req.request.params.get('search')).toBe('refund');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({});
  });

  it('getMessages omits the search param when not provided', () => {
    service.getMessages({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/support-messages'));
    expect(req.request.params.has('search')).toBe(false);
    req.flush({});
  });

  it('getMessage GETs /admin/support-messages/{id}', () => {
    service.getMessage('msg-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/support-messages/msg-1'));
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('reply POSTs /admin/support-messages/{id}/reply with the reply text', () => {
    service.reply('msg-1', 'Here is the answer.').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/support-messages/msg-1/reply'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ replyMessage: 'Here is the answer.' });
    req.flush(null);
  });

  it('deleteMessage DELETEs /admin/support-messages/{id}', () => {
    service.deleteMessage('msg-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/support-messages/msg-1'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('deleteMessages DELETEs /admin/support-messages with the ids in the body', () => {
    service.deleteMessages(['m-1', 'm-2']).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/support-messages'));
    expect(req.request.method).toBe('DELETE');
    expect(req.request.body).toEqual({ ids: ['m-1', 'm-2'] });
    req.flush(null);
  });
});
