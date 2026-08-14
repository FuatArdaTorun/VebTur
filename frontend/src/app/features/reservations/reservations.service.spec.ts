import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ReservationsService } from './reservations.service';
import { CreateReservationRequest, UpdateReservationRequest } from './models/reservation.model';

describe('ReservationsService', () => {
  let service: ReservationsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ReservationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('create POSTs to /reservation-requests', () => {
    const dto: CreateReservationRequest = {
      hotelId: 'h1',
      roomTypeId: 'r1',
      guestFullName: 'Jane',
      guestEmail: 'jane@example.com',
      guestPhone: '+90 555 000 00 00',
      checkInDate: '2026-09-01',
      checkOutDate: '2026-09-03',
      adultCount: 2,
      childCount: 0,
      specialRequests: null,
    };

    service.create(dto).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/reservation-requests'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush({});
  });

  it('getByReference GETs /reservation-requests/{reference}', () => {
    service.getByReference('VEB-ABC12345').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/reservation-requests/VEB-ABC12345'));
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('getMine GETs /reservation-requests/mine with paging params', () => {
    service.getMine(2, 20).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/reservation-requests/mine'));
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({});
  });

  it('updateMine PUTs to /reservation-requests/mine/{id}', () => {
    const dto: UpdateReservationRequest = {
      roomTypeId: 'r1',
      checkInDate: '2026-09-01',
      checkOutDate: '2026-09-03',
      adultCount: 2,
      childCount: 0,
      specialRequests: null,
    };

    service.updateMine('res-1', dto).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/reservation-requests/mine/res-1'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dto);
    req.flush({});
  });

  it('cancelMine POSTs to /reservation-requests/mine/{id}/cancel', () => {
    service.cancelMine('res-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/reservation-requests/mine/res-1/cancel'));
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
