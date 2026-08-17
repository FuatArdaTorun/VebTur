import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { HotelList } from './hotel-list';
import { HotelsService } from '../hotels.service';

const EMPTY_PAGE = { items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 };

describe('HotelList', () => {
  let fixture: ComponentFixture<HotelList>;
  let component: HotelList;
  let serviceStub: { getHotels: ReturnType<typeof vi.fn>; getAmenities: ReturnType<typeof vi.fn> };

  function createComponent(queryParams: Record<string, string> = {}): void {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of(EMPTY_PAGE)),
      getAmenities: vi.fn().mockReturnValue(of([])),
    };

    TestBed.configureTestingModule({
      imports: [HotelList],
      providers: [
        provideRouter([]),
        { provide: HotelsService, useValue: serviceStub },
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
});
