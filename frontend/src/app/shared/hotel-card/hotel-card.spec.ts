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
      customerRating: 4.333,
      customerReviewCount: 3,
    });
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders both the Google rating (with count) and the VebTur rating (with count)', () => {
    fixture.detectChanges();

    const lines: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('.hotel-card__rating'));
    expect(lines.length).toBe(2);

    const [googleLine, vebturLine] = lines;
    expect(googleLine.querySelector('.hotel-card__rating-value')!.textContent!.trim()).toBe('4.5');
    expect(googleLine.querySelector('.hotel-card__rating-source')!.textContent!.trim()).toBe('Google');
    expect(googleLine.querySelector('.hotel-card__rating-count')!.textContent!.trim()).toBe('(1,234)');

    expect(vebturLine.querySelector('.hotel-card__rating-value')!.textContent!.trim()).toBe('4.3');
    expect(vebturLine.querySelector('.hotel-card__rating-source')!.textContent!.trim()).toBe('VebTur');
    expect(vebturLine.querySelector('.hotel-card__rating-count')!.textContent!.trim()).toBe('(3)');
  });

  it('omits the VebTur rating line when the hotel has no VebTur reviews yet', () => {
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
      customerRating: null,
      customerReviewCount: 0,
    });
    fixture.detectChanges();

    const lines = fixture.nativeElement.querySelectorAll('.hotel-card__rating');
    expect(lines.length).toBe(1);
    expect(lines[0].querySelector('.hotel-card__rating-source').textContent.trim()).toBe('Google');
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
