import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { HotelCard } from './hotel-card';

describe('HotelCard', () => {
  let component: HotelCard;
  let fixture: ComponentFixture<HotelCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HotelCard],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(HotelCard);
    fixture.componentRef.setInput('hotel', {
      id: '1',
      name: 'Test Hotel',
      slug: 'test-hotel',
      city: 'Antalya',
      country: 'Turkey',
      starRating: 4,
      googleRating: 4.5,
      googleRatingCount: 1234,
      thumbnailUrl: null,
      startingNightlyPrice: 1000,
      currency: 'TRY',
    });
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('does not render a favorite button when showFavorite is false (the default)', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.hotel-card__favorite')).toBeNull();
  });

  it('renders a favorite button reflecting the favorited state when showFavorite is true', () => {
    fixture.componentRef.setInput('showFavorite', true);
    fixture.componentRef.setInput('favorited', true);
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('.hotel-card__favorite');
    expect(button).not.toBeNull();
    expect(button.classList.contains('hotel-card__favorite--active')).toBe(true);
    expect(button.getAttribute('aria-pressed')).toBe('true');
  });

  it('emits favoriteToggle and does not navigate when the favorite button is clicked', () => {
    fixture.componentRef.setInput('showFavorite', true);
    fixture.detectChanges();
    const emitted = vi.fn();
    component.favoriteToggle.subscribe(emitted);

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('.hotel-card__favorite');
    const clickEvent = new MouseEvent('click', { cancelable: true, bubbles: true });
    const preventDefaultSpy = vi.spyOn(clickEvent, 'preventDefault');
    button.dispatchEvent(clickEvent);

    expect(emitted).toHaveBeenCalledTimes(1);
    expect(preventDefaultSpy).toHaveBeenCalled();
  });
});
