import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { HotelDetail } from './hotel-detail';
import { HotelsService } from '../hotels.service';
import { AuthService } from '../../../core/auth/auth.service';
import { HotelDetail as HotelDetailModel, HotelReviewsResponse, Review } from '../models/hotel.model';

function buildHotel(overrides: Partial<HotelDetailModel> = {}): HotelDetailModel {
  return {
    id: 'hotel-1',
    name: 'Test Hotel',
    slug: 'test-hotel',
    description: 'A hotel.',
    city: 'Antalya',
    country: 'Turkey',
    address: 'Test Address',
    latitude: 36.9,
    longitude: 30.7,
    starRating: null,
    googleRating: null,
    googleRatingCount: null,
    officialWebsiteUrl: null,
    phoneNumber: null,
    images: [],
    amenities: [],
    roomTypes: [],
    customerRating: null,
    customerReviewCount: 0,
    ...overrides,
  };
}

function buildReview(overrides: Partial<Review> = {}): Review {
  return {
    id: 'review-1',
    rating: 5,
    comment: 'Loved it here.',
    reviewerName: 'Jane Guest',
    roomTypeName: 'Standard Room',
    checkInDate: '2026-01-05',
    checkOutDate: '2026-01-08',
    createdAtUtc: '2026-01-09T00:00:00Z',
    ...overrides,
  };
}

const EMPTY_REVIEWS: HotelReviewsResponse = { reviews: [], myReviewableReservations: [] };

describe('HotelDetail', () => {
  let component: HotelDetail;
  let fixture: ComponentFixture<HotelDetail>;
  let serviceStub: {
    getHotel: ReturnType<typeof vi.fn>;
    getExternalRating: ReturnType<typeof vi.fn>;
    getHotelReviews: ReturnType<typeof vi.fn>;
    createReview: ReturnType<typeof vi.fn>;
  };
  let authServiceStub: { isAuthenticated: ReturnType<typeof vi.fn> };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [HotelDetail],
      providers: [
        provideRouter([]),
        { provide: HotelsService, useValue: serviceStub },
        { provide: AuthService, useValue: authServiceStub },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ idOrSlug: 'test-hotel' }) } } },
      ],
    });

    fixture = TestBed.createComponent(HotelDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    authServiceStub = { isAuthenticated: vi.fn().mockReturnValue(false) };
  });

  it('loads the hotel, then fetches its external rating using the resolved id', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel())),
      getExternalRating: vi.fn().mockReturnValue(of({ provider: 'Manual', rating: 4.7, maximumRating: 5, reviewCount: 4158, isDemoData: true, lastUpdatedAtUtc: '2026-08-14T00:00:00Z' })),
      getHotelReviews: vi.fn().mockReturnValue(of(EMPTY_REVIEWS)),
      createReview: vi.fn(),
    };
    createComponent();

    expect(component['hotel']()?.id).toBe('hotel-1');
    expect(serviceStub.getExternalRating).toHaveBeenCalledWith('hotel-1');
    expect(component['externalRating']()?.rating).toBe(4.7);
    expect(component['externalRatingLoading']()).toBe(false);
  });

  it('renders the demo data badge when the rating came from the manual fallback', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel())),
      getExternalRating: vi.fn().mockReturnValue(of({ provider: 'Manual', rating: 4.7, maximumRating: 5, reviewCount: 4158, isDemoData: true, lastUpdatedAtUtc: '2026-08-14T00:00:00Z' })),
      getHotelReviews: vi.fn().mockReturnValue(of(EMPTY_REVIEWS)),
      createReview: vi.fn(),
    };
    createComponent();

    expect(fixture.nativeElement.textContent).toContain('Demo data');
    expect(fixture.nativeElement.textContent).toContain('4.7');
  });

  it('does not render a demo badge for a live rating', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel())),
      getExternalRating: vi.fn().mockReturnValue(of({ provider: 'Google', rating: 4.6, maximumRating: 5, reviewCount: 900, isDemoData: false, lastUpdatedAtUtc: '2026-08-14T00:00:00Z' })),
      getHotelReviews: vi.fn().mockReturnValue(of(EMPTY_REVIEWS)),
      createReview: vi.fn(),
    };
    createComponent();

    expect(fixture.nativeElement.textContent).not.toContain('Demo data');
  });

  it('shows no rating block when no provider has data (204)', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel())),
      getExternalRating: vi.fn().mockReturnValue(of(null)),
      getHotelReviews: vi.fn().mockReturnValue(of(EMPTY_REVIEWS)),
      createReview: vi.fn(),
    };
    createComponent();

    expect(component['externalRating']()).toBeNull();
    expect(fixture.nativeElement.querySelector('.hotel-detail__rating')).toBeNull();
  });

  it('a failed external rating fetch does not break the page', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel())),
      getExternalRating: vi.fn().mockReturnValue(throwError(() => new Error('boom'))),
      getHotelReviews: vi.fn().mockReturnValue(of(EMPTY_REVIEWS)),
      createReview: vi.fn(),
    };
    createComponent();

    expect(component['error']()).toBe(false);
    expect(component['externalRatingLoading']()).toBe(false);
    expect(component['externalRating']()).toBeNull();
  });

  it('shows the error state when the main hotel fetch fails', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(throwError(() => new Error('boom'))),
      getExternalRating: vi.fn(),
      getHotelReviews: vi.fn(),
      createReview: vi.fn(),
    };
    createComponent();

    expect(component['error']()).toBe(true);
    expect(serviceStub.getExternalRating).not.toHaveBeenCalled();
    expect(serviceStub.getHotelReviews).not.toHaveBeenCalled();
  });

  it('renders the customer rating summary and the review list', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel({ customerRating: 4.5, customerReviewCount: 2 }))),
      getExternalRating: vi.fn().mockReturnValue(of(null)),
      getHotelReviews: vi.fn().mockReturnValue(of({ reviews: [buildReview(), buildReview({ id: 'review-2', comment: null })], myReviewableReservations: [] })),
      createReview: vi.fn(),
    };
    createComponent();

    expect(fixture.nativeElement.textContent).toContain('4.5');
    expect(fixture.nativeElement.textContent).toContain('2 reviews');
    expect(fixture.nativeElement.querySelectorAll('.hotel-detail__review').length).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('Loved it here.');
  });

  it('shows a sign-in hint (not the form) when unauthenticated with nothing to review', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel())),
      getExternalRating: vi.fn().mockReturnValue(of(null)),
      getHotelReviews: vi.fn().mockReturnValue(of(EMPTY_REVIEWS)),
      createReview: vi.fn(),
    };
    createComponent();

    expect(fixture.nativeElement.querySelector('.hotel-detail__review-form')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Sign in');
  });

  it('shows the write-a-review form when a reviewable reservation exists, pre-selected', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel())),
      getExternalRating: vi.fn().mockReturnValue(of(null)),
      getHotelReviews: vi.fn().mockReturnValue(
        of({
          reviews: [],
          myReviewableReservations: [{ reservationRequestId: 'res-1', roomTypeName: 'Standard Room', checkInDate: '2026-01-05', checkOutDate: '2026-01-08' }],
        }),
      ),
      createReview: vi.fn(),
    };
    authServiceStub.isAuthenticated.mockReturnValue(true);
    createComponent();

    expect(fixture.nativeElement.querySelector('.hotel-detail__review-form')).not.toBeNull();
    expect(component['reviewForm'].controls.reservationRequestId.value).toBe('res-1');
  });

  it('submitting a review posts the dto, prepends it to the list, and clears eligibility for that reservation', () => {
    const created = buildReview({ id: 'new-review', rating: 5, comment: 'Amazing.' });
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel())),
      getExternalRating: vi.fn().mockReturnValue(of(null)),
      getHotelReviews: vi.fn().mockReturnValue(
        of({
          reviews: [],
          myReviewableReservations: [{ reservationRequestId: 'res-1', roomTypeName: 'Standard Room', checkInDate: '2026-01-05', checkOutDate: '2026-01-08' }],
        }),
      ),
      createReview: vi.fn().mockReturnValue(of(created)),
    };
    authServiceStub.isAuthenticated.mockReturnValue(true);
    createComponent();

    component['setRating'](5);
    component['reviewForm'].controls.comment.setValue('Amazing.');
    component['submitReview']();

    expect(serviceStub.createReview).toHaveBeenCalledWith('hotel-1', { reservationRequestId: 'res-1', rating: 5, comment: 'Amazing.' });
    expect(component['reviews']()).toContainEqual(created);
    expect(component['reviewableReservations']()).toEqual([]);
    // The hotel's aggregate was fetched before this review existed — it should be patched in
    // place rather than left stale until a reload.
    expect(component['hotel']()?.customerReviewCount).toBe(1);
    expect(component['hotel']()?.customerRating).toBe(5);
  });

  it('does not submit without a chosen star rating', () => {
    serviceStub = {
      getHotel: vi.fn().mockReturnValue(of(buildHotel())),
      getExternalRating: vi.fn().mockReturnValue(of(null)),
      getHotelReviews: vi.fn().mockReturnValue(
        of({
          reviews: [],
          myReviewableReservations: [{ reservationRequestId: 'res-1', roomTypeName: 'Standard Room', checkInDate: '2026-01-05', checkOutDate: '2026-01-08' }],
        }),
      ),
      createReview: vi.fn(),
    };
    authServiceStub.isAuthenticated.mockReturnValue(true);
    createComponent();

    component['submitReview']();

    expect(serviceStub.createReview).not.toHaveBeenCalled();
  });
});
