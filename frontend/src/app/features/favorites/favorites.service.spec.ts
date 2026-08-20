import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { FavoritesService } from './favorites.service';

describe('FavoritesService', () => {
  let service: FavoritesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(FavoritesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getMine GETs /favorites/mine with page and pageSize', () => {
    service.getMine(2, 12).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/favorites/mine'));
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('12');
    req.flush({ items: [], page: 2, pageSize: 12, totalCount: 0, totalPages: 0 });
  });

  it('getMineIds GETs /favorites/mine/ids', () => {
    service.getMineIds().subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/favorites/mine/ids'));
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('addFavorite POSTs /hotels/{id}/favorite', () => {
    service.addFavorite('hotel-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/hotels/hotel-1/favorite'));
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('removeFavorite DELETEs /hotels/{id}/favorite', () => {
    service.removeFavorite('hotel-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/hotels/hotel-1/favorite'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
