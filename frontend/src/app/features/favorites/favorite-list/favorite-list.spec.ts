import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { FavoriteList } from './favorite-list';
import { FavoritesService } from '../favorites.service';

const HOTEL = {
  id: 'hotel-1',
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
};

describe('FavoriteList', () => {
  let fixture: ComponentFixture<FavoriteList>;
  let component: FavoriteList;
  let serviceStub: { getMine: ReturnType<typeof vi.fn>; removeFavorite: ReturnType<typeof vi.fn> };

  function createComponent(getMineReturn = of({ items: [HOTEL], page: 1, pageSize: 12, totalCount: 1, totalPages: 1 })): void {
    serviceStub = {
      getMine: vi.fn().mockReturnValue(getMineReturn),
      removeFavorite: vi.fn().mockReturnValue(of(undefined)),
    };

    TestBed.configureTestingModule({
      imports: [FavoriteList],
      providers: [provideRouter([]), { provide: FavoritesService, useValue: serviceStub }],
    });

    fixture = TestBed.createComponent(FavoriteList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads and displays the favorited hotels on init', () => {
    createComponent();

    expect(component['hotels']()).toEqual([HOTEL]);
    expect(component['loading']()).toBe(false);
    expect(component['error']()).toBe(false);
  });

  it('shows the error state when the fetch fails', () => {
    createComponent(throwError(() => new Error('boom')));

    expect(component['error']()).toBe(true);
    expect(component['loading']()).toBe(false);
  });

  it('shows the empty state when there are no favorites', () => {
    createComponent(of({ items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 }));

    expect(fixture.nativeElement.querySelector('app-empty-state')).toBeTruthy();
  });

  it('removeFavorite calls the service and drops the hotel from the local list', () => {
    createComponent();

    component['removeFavorite']('hotel-1');

    expect(serviceStub.removeFavorite).toHaveBeenCalledWith('hotel-1');
    expect(component['hotels']()).toEqual([]);
  });

  it('goToPage refetches for a valid in-range page and ignores an out-of-range one', () => {
    createComponent(of({ items: [HOTEL], page: 1, pageSize: 12, totalCount: 24, totalPages: 2 }));
    serviceStub.getMine.mockClear();

    component['goToPage'](0);
    expect(serviceStub.getMine).not.toHaveBeenCalled();

    component['goToPage'](2);
    expect(serviceStub.getMine).toHaveBeenCalledWith(2, 12);
  });
});
