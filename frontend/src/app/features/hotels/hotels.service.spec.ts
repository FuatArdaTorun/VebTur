import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { HotelsService } from './hotels.service';

describe('HotelsService', () => {
  let service: HotelsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(HotelsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getHotels sends every provided filter/sort param, including repeated amenity slugs', () => {
    service
      .getHotels({
        city: 'Antalya',
        search: 'Sherwood',
        minPrice: 1000,
        maxPrice: 5000,
        minStarRating: 4,
        minCapacity: 2,
        sort: 'price-asc',
        amenities: ['wifi', 'pool'],
        page: 2,
        pageSize: 12,
      })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/hotels'));
    expect(req.request.params.get('city')).toBe('Antalya');
    expect(req.request.params.get('search')).toBe('Sherwood');
    expect(req.request.params.get('minPrice')).toBe('1000');
    expect(req.request.params.get('maxPrice')).toBe('5000');
    expect(req.request.params.get('minStarRating')).toBe('4');
    expect(req.request.params.get('minCapacity')).toBe('2');
    expect(req.request.params.get('sort')).toBe('price-asc');
    expect(req.request.params.getAll('amenities')).toEqual(['wifi', 'pool']);
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('12');
    req.flush({ items: [], page: 2, pageSize: 12, totalCount: 0 });
  });

  it('getHotels omits every optional param when not provided', () => {
    service.getHotels({ page: 1, pageSize: 12 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/hotels'));
    expect(req.request.params.has('city')).toBe(false);
    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('minPrice')).toBe(false);
    expect(req.request.params.has('maxPrice')).toBe(false);
    expect(req.request.params.has('minStarRating')).toBe(false);
    expect(req.request.params.has('minCapacity')).toBe(false);
    expect(req.request.params.has('sort')).toBe(false);
    expect(req.request.params.has('amenities')).toBe(false);
    req.flush({ items: [], page: 1, pageSize: 12, totalCount: 0 });
  });

  it('getHotel GETs /hotels/{idOrSlug}', () => {
    service.getHotel('sherwood-exclusive-kemer').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/hotels/sherwood-exclusive-kemer'));
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('getAmenities GETs /amenities', () => {
    service.getAmenities().subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/amenities'));
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('getExternalRating GETs /hotels/{id}/external-rating', () => {
    service.getExternalRating('hotel-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/hotels/hotel-1/external-rating'));
    expect(req.request.method).toBe('GET');
    req.flush(null);
  });

  it('getHotelReviews GETs /hotels/{id}/reviews', () => {
    service.getHotelReviews('hotel-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/hotels/hotel-1/reviews'));
    expect(req.request.method).toBe('GET');
    req.flush({ reviews: [], averageRating: null, reviewCount: 0, reviewableReservations: [] });
  });

  it('createReview POSTs /hotels/{id}/reviews with the given body', () => {
    const dto = { reservationRequestId: 'res-1', rating: 5, comment: 'Great stay' };
    service.createReview('hotel-1', dto).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/hotels/hotel-1/reviews'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush({});
  });
});
