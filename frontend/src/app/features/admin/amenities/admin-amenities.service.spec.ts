import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdminAmenitiesService } from './admin-amenities.service';

describe('AdminAmenitiesService', () => {
  let service: AdminAmenitiesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminAmenitiesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAmenities GETs /admin/amenities', () => {
    service.getAmenities().subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/amenities'));
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('createAmenity POSTs /admin/amenities with the given body', () => {
    const dto = { name: 'Spa', slug: 'spa', iconKey: null };
    service.createAmenity(dto).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/amenities'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush({});
  });

  it('updateAmenity PUTs /admin/amenities/{id} with the given body', () => {
    const dto = { name: 'Spa', slug: 'spa', iconKey: null };
    service.updateAmenity('amenity-1', dto).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/amenities/amenity-1'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dto);
    req.flush({});
  });

  it('deleteAmenity DELETEs /admin/amenities/{id}', () => {
    service.deleteAmenity('amenity-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/amenities/amenity-1'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('deleteAmenities DELETEs /admin/amenities with the ids in the body', () => {
    service.deleteAmenities(['a-1', 'a-2']).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/amenities'));
    expect(req.request.method).toBe('DELETE');
    expect(req.request.body).toEqual({ ids: ['a-1', 'a-2'] });
    req.flush(null);
  });
});
