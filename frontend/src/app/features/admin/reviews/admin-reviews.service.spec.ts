import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdminReviewsService } from './admin-reviews.service';

describe('AdminReviewsService', () => {
  let service: AdminReviewsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminReviewsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getReviews GETs /admin/reviews with search when provided', () => {
    service.getReviews({ search: 'great stay', page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reviews'));
    expect(req.request.params.get('search')).toBe('great stay');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({});
  });

  it('getReviews omits the search param when not provided', () => {
    service.getReviews({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reviews'));
    expect(req.request.params.has('search')).toBe(false);
    req.flush({});
  });

  it('hideReview POSTs /admin/reviews/{id}/hide', () => {
    service.hideReview('review-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reviews/review-1/hide'));
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('unhideReview POSTs /admin/reviews/{id}/unhide', () => {
    service.unhideReview('review-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reviews/review-1/unhide'));
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('deleteReview DELETEs /admin/reviews/{id}', () => {
    service.deleteReview('review-1').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reviews/review-1'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('deleteReviews DELETEs /admin/reviews with the ids in the body', () => {
    service.deleteReviews(['r-1', 'r-2']).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reviews'));
    expect(req.request.method).toBe('DELETE');
    expect(req.request.body).toEqual({ ids: ['r-1', 'r-2'] });
    req.flush(null);
  });
});
