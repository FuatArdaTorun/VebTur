import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AdminReviewList } from './admin-review-list';
import { AdminReviewsService } from '../admin-reviews.service';
import { AdminReviewSummary } from '../models/admin-review.model';

function buildReview(overrides: Partial<AdminReviewSummary> = {}): AdminReviewSummary {
  return {
    id: 'r-1',
    hotelId: 'hotel-1',
    hotelName: 'Test Hotel',
    reviewerName: 'Jane Guest',
    rating: 5,
    comment: 'Great stay.',
    roomTypeName: 'Standard Room',
    checkInDate: '2026-01-05',
    checkOutDate: '2026-01-08',
    isHidden: false,
    createdAtUtc: '2026-01-09T00:00:00Z',
    ...overrides,
  };
}

describe('AdminReviewList', () => {
  let fixture: ComponentFixture<AdminReviewList>;
  let component: AdminReviewList;
  let serviceStub: {
    getReviews: ReturnType<typeof vi.fn>;
    hideReview?: ReturnType<typeof vi.fn>;
    unhideReview?: ReturnType<typeof vi.fn>;
    deleteReviews?: ReturnType<typeof vi.fn>;
  };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [AdminReviewList],
      providers: [provideRouter([]), { provide: AdminReviewsService, useValue: serviceStub }],
    });

    fixture = TestBed.createComponent(AdminReviewList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads and displays reviews on init', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [buildReview()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    expect(component['reviews']().length).toBe(1);
    expect(component['loading']()).toBe(false);
  });

  it('shows the error state when the fetch fails', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) };
    createComponent();

    expect(component['error']()).toBe(true);
  });

  it('starts with an empty search box and no filter on the initial fetch', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    expect(component['searchControl'].value).toBe('');
    expect(serviceStub.getReviews).toHaveBeenCalledWith(expect.objectContaining({ search: undefined, page: 1 }));
  });

  it('applying the search re-fetches with the typed term, resetting to page 1', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();
    component['page'].set(3);

    component['searchControl'].setValue('Test Hotel');
    component['applySearch']();

    expect(serviceStub.getReviews).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'Test Hotel', page: 1 }));
  });

  it('goToPage ignores out-of-range pages', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 2 })) };
    createComponent();
    const callsBefore = serviceStub.getReviews.mock.calls.length;

    component['goToPage'](0);
    component['goToPage'](99);

    expect(serviceStub.getReviews.mock.calls.length).toBe(callsBefore);
  });

  it('selection mode is off by default, with no checkboxes rendered', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [buildReview()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    expect(component['selectionMode']()).toBe(false);
    expect(fixture.nativeElement.querySelector('input[type="checkbox"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Delete Review');
  });

  it('toggling selection mode on shows checkboxes; toggling it off again clears any selection', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [buildReview()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    component['toggleSelectionMode']();
    fixture.detectChanges();
    expect(component['selectionMode']()).toBe(true);
    expect(fixture.nativeElement.querySelector('input[type="checkbox"]')).not.toBeNull();

    component['toggleSelect']('r-1');
    expect(component['selectedCount']()).toBe(1);

    component['toggleSelectionMode']();
    expect(component['selectionMode']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
  });

  it('select-all selects every currently loaded row, and toggles them all off again', () => {
    serviceStub = {
      getReviews: vi.fn().mockReturnValue(of({ items: [buildReview({ id: 'r-1' }), buildReview({ id: 'r-2' })], page: 1, pageSize: 20, totalCount: 2, totalPages: 1 })),
    };
    createComponent();

    component['toggleSelectAll']();
    expect(component['isAllSelected']()).toBe(true);
    expect(component['selectedCount']()).toBe(2);

    component['toggleSelectAll']();
    expect(component['isAllSelected']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
  });

  it('requestBulkDelete opens the confirm dialog only when something is selected', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [buildReview()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    component['requestBulkDelete']();
    expect(component['confirmingBulkDelete']()).toBe(false);

    component['toggleSelect']('r-1');
    component['requestBulkDelete']();
    expect(component['confirmingBulkDelete']()).toBe(true);
  });

  it('confirming bulk delete calls the service with the selected ids, clears selection, and refetches', () => {
    serviceStub = {
      getReviews: vi.fn().mockReturnValue(of({ items: [buildReview({ id: 'r-1' }), buildReview({ id: 'r-2' })], page: 1, pageSize: 20, totalCount: 2, totalPages: 1 })),
      deleteReviews: vi.fn().mockReturnValue(of(undefined)),
    };
    createComponent();
    component['toggleSelect']('r-1');
    component['requestBulkDelete']();

    component['confirmBulkDelete']();

    expect(serviceStub.deleteReviews).toHaveBeenCalledWith(['r-1']);
    expect(component['confirmingBulkDelete']()).toBe(false);
    expect(component['selectionMode']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
    expect(serviceStub.getReviews).toHaveBeenCalledTimes(2);
  });

  it('toggleHidden hides a visible review and refetches', () => {
    serviceStub = {
      getReviews: vi.fn().mockReturnValue(of({ items: [buildReview({ isHidden: false })], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      hideReview: vi.fn().mockReturnValue(of(undefined)),
    };
    createComponent();

    component['toggleHidden'](buildReview({ isHidden: false }));

    expect(serviceStub.hideReview).toHaveBeenCalledWith('r-1');
    expect(serviceStub.getReviews).toHaveBeenCalledTimes(2);
  });

  it('toggleHidden unhides a hidden review', () => {
    serviceStub = {
      getReviews: vi.fn().mockReturnValue(of({ items: [buildReview({ isHidden: true })], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      unhideReview: vi.fn().mockReturnValue(of(undefined)),
    };
    createComponent();

    component['toggleHidden'](buildReview({ isHidden: true }));

    expect(serviceStub.unhideReview).toHaveBeenCalledWith('r-1');
  });

  it('clicking a column header sorts ascending by that column and resets to page 1', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();
    component['page'].set(3);

    component['toggleSort']('hotel');

    expect(component['sort']()).toBe('hotel-asc');
    expect(component['sortIndicator']('hotel')).toBe('▲');
    expect(serviceStub.getReviews).toHaveBeenLastCalledWith(expect.objectContaining({ sort: 'hotel-asc', page: 1 }));
  });

  it('clicking the same column header again toggles to descending', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    component['toggleSort']('rating');
    component['toggleSort']('rating');

    expect(component['sort']()).toBe('rating-desc');
    expect(component['sortIndicator']('rating')).toBe('▼');
  });

  it('switching to a different column starts ascending again', () => {
    serviceStub = { getReviews: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    component['toggleSort']('status');
    component['toggleSort']('status');
    component['toggleSort']('reviewer');

    expect(component['sort']()).toBe('reviewer-asc');
    expect(component['sortIndicator']('status')).toBe('');
  });
});
