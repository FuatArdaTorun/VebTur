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
});
