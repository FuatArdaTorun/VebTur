import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdminHotelsService } from './admin-hotels.service';

describe('AdminHotelsService', () => {
  let service: AdminHotelsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminHotelsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getHotels GETs /admin/hotels with search and isActive when provided', () => {
    service.getHotels({ search: 'Sherwood', isActive: false, page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/hotels'));
    expect(req.request.params.get('search')).toBe('Sherwood');
    expect(req.request.params.get('isActive')).toBe('false');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({});
  });

  it('getHotels omits search and isActive when not provided', () => {
    service.getHotels({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/hotels'));
    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('isActive')).toBe(false);
    req.flush({});
  });

  it('getHotel GETs /admin/hotels/{id}', () => {
    service.getHotel('hotel-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/hotels/hotel-1'));
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('createHotel POSTs /admin/hotels with the given body', () => {
    const dto = { name: 'Test Hotel' } as never;
    service.createHotel(dto).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/hotels'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush({});
  });

  it('updateHotel PUTs /admin/hotels/{id} with the given body', () => {
    const dto = { name: 'Test Hotel' } as never;
    service.updateHotel('hotel-1', dto).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/hotels/hotel-1'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dto);
    req.flush({});
  });

  it('deactivateHotel DELETEs /admin/hotels/{id}', () => {
    service.deactivateHotel('hotel-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/hotels/hotel-1'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('reactivateHotel POSTs /admin/hotels/{id}/reactivate', () => {
    service.reactivateHotel('hotel-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/hotels/hotel-1/reactivate'));
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('deleteHotelPermanently DELETEs /admin/hotels/{id}/permanent', () => {
    service.deleteHotelPermanently('hotel-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/hotels/hotel-1/permanent'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('deleteHotelsPermanently DELETEs /admin/hotels/permanent with the ids in the body', () => {
    service.deleteHotelsPermanently(['h-1', 'h-2']).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/hotels/permanent'));
    expect(req.request.method).toBe('DELETE');
    expect(req.request.body).toEqual({ ids: ['h-1', 'h-2'] });
    req.flush(null);
  });
});
