import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router, provideRouter } from '@angular/router';
import { Observable, of, Subject, throwError } from 'rxjs';

import { HotelList } from './hotel-list';
import { HotelsService } from '../hotels.service';
import { AuthService } from '../../../core/auth/auth.service';
import { FavoritesService } from '../../favorites/favorites.service';

const EMPTY_PAGE = { items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 };

const MULTI_PAGE = {
  items: [
    {
      id: 'h1',
      name: 'Test Hotel',
      slug: 'test-hotel',
      city: 'Antalya',
      country: 'Turkey',
      starRating: 5,
      googleRating: 4.5,
      googleRatingCount: 100,
      thumbnailUrl: null,
      startingNightlyPrice: 1000,
      currency: 'TRY',
    },
  ],
  page: 1,
  pageSize: 12,
  totalCount: 30,
  totalPages: 3,
};

describe('HotelList', () => {
  let fixture: ComponentFixture<HotelList>;
  let component: HotelList;
  let serviceStub: { getHotels: ReturnType<typeof vi.fn>; getAmenities: ReturnType<typeof vi.fn> };
  let authServiceStub: { isAuthenticated: ReturnType<typeof vi.fn> };
  let favoritesServiceStub: { getMineIds: ReturnType<typeof vi.fn>; addFavorite: ReturnType<typeof vi.fn>; removeFavorite: ReturnType<typeof vi.fn> };

  function createComponent(
    queryParams: Record<string, string> = {},
    getHotelsReturn: Observable<unknown> = of(EMPTY_PAGE),
    options: { isAuthenticated?: boolean; favoriteIds?: string[] } = {},
  ): void {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(getHotelsReturn),
      getAmenities: vi.fn().mockReturnValue(of([])),
    };
    authServiceStub = { isAuthenticated: vi.fn().mockReturnValue(options.isAuthenticated ?? false) };
    favoritesServiceStub = {
      getMineIds: vi.fn().mockReturnValue(of(options.favoriteIds ?? [])),
      addFavorite: vi.fn().mockReturnValue(of(undefined)),
      removeFavorite: vi.fn().mockReturnValue(of(undefined)),
    };

    TestBed.configureTestingModule({
      imports: [HotelList],
      providers: [
        provideRouter([]),
        { provide: HotelsService, useValue: serviceStub },
        { provide: AuthService, useValue: authServiceStub },
        { provide: FavoritesService, useValue: favoritesServiceStub },
        { provide: ActivatedRoute, useValue: { queryParamMap: of(convertToParamMap(queryParams)) } },
      ],
    });

    fixture = TestBed.createComponent(HotelList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('should create', () => {
    createComponent();

    expect(component).toBeTruthy();
  });

  it('pre-fills the search box from a "search" route query param and includes it in the initial hotel fetch', () => {
    createComponent({ search: 'Selectum' });

    expect(component['filterForm'].value.search).toBe('Selectum');
    expect(serviceStub.getHotels).toHaveBeenCalledWith(expect.objectContaining({ search: 'Selectum' }));
  });

  it('sends no search param when arriving without one', () => {
    createComponent();

    expect(serviceStub.getHotels).toHaveBeenCalledWith(expect.objectContaining({ search: undefined }));
  });

  it('applyFilters navigates with the typed search term as a query param', () => {
    createComponent();
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component['filterForm'].patchValue({ search: 'Selectum' });
    component['applyFilters']();

    expect(navigateSpy).toHaveBeenCalledWith([], expect.objectContaining({ queryParams: expect.objectContaining({ search: 'Selectum' }) }));
  });

  it('shows the loading state while the fetch is in flight, then the result once it resolves', () => {
    const subject = new Subject<unknown>();
    createComponent({}, subject.asObservable());

    expect(fixture.nativeElement.querySelector('app-loading-state')).toBeTruthy();
    expect(component['loading']()).toBe(true);

    subject.next(EMPTY_PAGE);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-loading-state')).toBeFalsy();
    expect(component['loading']()).toBe(false);
  });

  it('shows the error state and stops loading when the hotel fetch fails', () => {
    createComponent({}, throwError(() => new Error('network error')));

    expect(component['error']()).toBe(true);
    expect(component['loading']()).toBe(false);
    expect(fixture.nativeElement.querySelector('app-error-state')).toBeTruthy();
  });

  it('shows the empty state when the fetch resolves with zero hotels', () => {
    createComponent();

    expect(component['hotels']()).toEqual([]);
    expect(component['totalCount']()).toBe(0);
    expect(fixture.nativeElement.querySelector('app-empty-state')).toBeTruthy();
  });

  it('renders hotel count and enabled pagination controls for a multi-page result', () => {
    createComponent({}, of(MULTI_PAGE));

    expect(component['hotels']()).toEqual(MULTI_PAGE.items);
    expect(component['totalCount']()).toBe(30);
    expect(component['totalPages']()).toBe(3);
    const nextButton: HTMLButtonElement = fixture.nativeElement.querySelector('.hotel-list__pagination button:last-child');
    expect(nextButton.disabled).toBe(false);
  });

  it('goToPage navigates to a valid in-range page', () => {
    createComponent({}, of(MULTI_PAGE));
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component['goToPage'](2);

    expect(navigateSpy).toHaveBeenCalledWith([], expect.objectContaining({ queryParams: expect.objectContaining({ page: 2 }) }));
  });

  it('goToPage does nothing for a page below 1 or beyond totalPages', () => {
    createComponent({}, of(MULTI_PAGE));
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component['goToPage'](0);
    component['goToPage'](4);

    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('toggleAmenity adds an unselected slug and removes an already-selected one', () => {
    createComponent();

    component['toggleAmenity']('wifi');
    expect(component['selectedAmenities']()).toEqual(['wifi']);

    component['toggleAmenity']('pool');
    expect(component['selectedAmenities']()).toEqual(['wifi', 'pool']);

    component['toggleAmenity']('wifi');
    expect(component['selectedAmenities']()).toEqual(['pool']);
  });

  it('applyFilters includes the selected amenities as a query param', () => {
    createComponent();
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component['toggleAmenity']('wifi');
    component['applyFilters']();

    expect(navigateSpy).toHaveBeenCalledWith(
      [],
      expect.objectContaining({ queryParams: expect.objectContaining({ amenities: ['wifi'] }) }),
    );
  });

  it('does not fetch favorite ids when the visitor is not authenticated', () => {
    createComponent({}, of(EMPTY_PAGE), { isAuthenticated: false });

    expect(favoritesServiceStub.getMineIds).not.toHaveBeenCalled();
  });

  it('fetches favorite ids on init when authenticated', () => {
    createComponent({}, of(EMPTY_PAGE), { isAuthenticated: true, favoriteIds: ['h1'] });

    expect(favoritesServiceStub.getMineIds).toHaveBeenCalled();
    expect(component['favoriteIds']()).toEqual(new Set(['h1']));
  });

  it('toggleFavorite adds an unfavorited hotel via the service and updates the local set', () => {
    createComponent({}, of(EMPTY_PAGE), { isAuthenticated: true, favoriteIds: [] });

    component['toggleFavorite']('h1');

    expect(favoritesServiceStub.addFavorite).toHaveBeenCalledWith('h1');
    expect(component['favoriteIds']().has('h1')).toBe(true);
  });

  it('toggleFavorite removes an already-favorited hotel via the service and updates the local set', () => {
    createComponent({}, of(EMPTY_PAGE), { isAuthenticated: true, favoriteIds: ['h1'] });

    component['toggleFavorite']('h1');

    expect(favoritesServiceStub.removeFavorite).toHaveBeenCalledWith('h1');
    expect(component['favoriteIds']().has('h1')).toBe(false);
  });
});
