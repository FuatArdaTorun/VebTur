import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdminNotificationsService } from './admin-notifications.service';

describe('AdminNotificationsService', () => {
  let service: AdminNotificationsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminNotificationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getNotifications GETs /admin/notifications with status and search', () => {
    service.getNotifications({ status: 'Sent', search: 'VEB-1234', page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/notifications'));
    expect(req.request.params.get('status')).toBe('Sent');
    expect(req.request.params.get('search')).toBe('VEB-1234');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({});
  });

  it('getNotifications omits status and search params when not provided', () => {
    service.getNotifications({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/notifications'));
    expect(req.request.params.has('status')).toBe(false);
    expect(req.request.params.has('search')).toBe(false);
    req.flush({});
  });

  it('deleteNotifications DELETEs /admin/notifications with the ids in the body', () => {
    service.deleteNotifications(['n-1', 'n-2']).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/notifications'));
    expect(req.request.method).toBe('DELETE');
    expect(req.request.body).toEqual({ ids: ['n-1', 'n-2'] });
    req.flush(null);
  });
});
