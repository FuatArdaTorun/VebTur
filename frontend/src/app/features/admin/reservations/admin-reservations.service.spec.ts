import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdminReservationsService } from './admin-reservations.service';

describe('AdminReservationsService', () => {
  let service: AdminReservationsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminReservationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getReservations GETs /admin/reservation-requests with status, hotelId, search, and sort', () => {
    service
      .getReservations({ status: 'Pending', hotelId: 'h1', search: 'jane@example.com', sort: 'hotel-asc', page: 1, pageSize: 20 })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests'));
    expect(req.request.params.get('status')).toBe('Pending');
    expect(req.request.params.get('hotelId')).toBe('h1');
    expect(req.request.params.get('search')).toBe('jane@example.com');
    expect(req.request.params.get('sort')).toBe('hotel-asc');
    req.flush({});
  });

  it('getReservation GETs /admin/reservation-requests/{id}', () => {
    service.getReservation('res-1').subscribe();

    httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests/res-1')).flush({});
  });

  it('confirm POSTs to /admin/reservation-requests/{id}/confirm', () => {
    service.confirm('res-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests/res-1/confirm'));
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('reject POSTs to /admin/reservation-requests/{id}/reject', () => {
    service.reject('res-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests/res-1/reject'));
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('cancel POSTs to /admin/reservation-requests/{id}/cancel', () => {
    service.cancel('res-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests/res-1/cancel'));
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('deleteReservation DELETEs /admin/reservation-requests/{id}', () => {
    service.deleteReservation('res-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests/res-1'));
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });
});
