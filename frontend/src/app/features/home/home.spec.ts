import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { Home } from './home';
import { HotelsService } from '../hotels/hotels.service';
import { AuthService } from '../../core/auth/auth.service';
import { FavoritesService } from '../favorites/favorites.service';

const HOTELS_PAGE = { items: [{ id: 'h1', name: 'Test Hotel', slug: 'test-hotel' }], page: 1, pageSize: 6, totalCount: 1, totalPages: 1 };

describe('Home', () => {
  let component: Home;
  let fixture: ComponentFixture<Home>;
  let hotelsServiceStub: { getHotels: ReturnType<typeof vi.fn> };
  let authServiceStub: { isAuthenticated: ReturnType<typeof vi.fn> };
  let favoritesServiceStub: { getMineIds: ReturnType<typeof vi.fn>; addFavorite: ReturnType<typeof vi.fn>; removeFavorite: ReturnType<typeof vi.fn> };

  function createComponent(options: { isAuthenticated?: boolean; favoriteIds?: string[] } = {}): void {
    hotelsServiceStub = { getHotels: vi.fn().mockReturnValue(of(HOTELS_PAGE)) };
    authServiceStub = { isAuthenticated: vi.fn().mockReturnValue(options.isAuthenticated ?? false) };
    favoritesServiceStub = {
      getMineIds: vi.fn().mockReturnValue(of(options.favoriteIds ?? [])),
      addFavorite: vi.fn().mockReturnValue(of(undefined)),
      removeFavorite: vi.fn().mockReturnValue(of(undefined)),
    };

    TestBed.configureTestingModule({
      imports: [Home],
      providers: [
        provideRouter([]),
        { provide: HotelsService, useValue: hotelsServiceStub },
        { provide: AuthService, useValue: authServiceStub },
        { provide: FavoritesService, useValue: favoritesServiceStub },
      ],
    });

    fixture = TestBed.createComponent(Home);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('should create', () => {
    createComponent();

    expect(component).toBeTruthy();
  });

  it('loads featured hotels on init', () => {
    createComponent();

    expect(component['featuredHotels']()).toEqual(HOTELS_PAGE.items);
  });

  it('does not fetch favorite ids when not authenticated', () => {
    createComponent({ isAuthenticated: false });

    expect(favoritesServiceStub.getMineIds).not.toHaveBeenCalled();
  });

  it('fetches favorite ids on init when authenticated', () => {
    createComponent({ isAuthenticated: true, favoriteIds: ['h1'] });

    expect(component['favoriteIds']()).toEqual(new Set(['h1']));
  });

  it('toggleFavorite adds an unfavorited hotel and updates the local set', () => {
    createComponent({ isAuthenticated: true, favoriteIds: [] });

    component['toggleFavorite']('h1');

    expect(favoritesServiceStub.addFavorite).toHaveBeenCalledWith('h1');
    expect(component['favoriteIds']().has('h1')).toBe(true);
  });

  it('toggleFavorite removes an already-favorited hotel and updates the local set', () => {
    createComponent({ isAuthenticated: true, favoriteIds: ['h1'] });

    component['toggleFavorite']('h1');

    expect(favoritesServiceStub.removeFavorite).toHaveBeenCalledWith('h1');
    expect(component['favoriteIds']().has('h1')).toBe(false);
  });
});
