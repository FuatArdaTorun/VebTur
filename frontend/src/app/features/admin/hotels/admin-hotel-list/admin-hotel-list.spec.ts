import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { WritableSignal } from '@angular/core';
import { of, throwError } from 'rxjs';

import { AdminHotelList } from './admin-hotel-list';
import { AdminHotelsService } from '../admin-hotels.service';
import { AdminHotelSummary } from '../models/admin-hotel.model';

interface AdminHotelListInternals {
  hotels: WritableSignal<AdminHotelSummary[]>;
  loading: WritableSignal<boolean>;
  error: WritableSignal<boolean>;
  pendingToggle: WritableSignal<AdminHotelSummary | null>;
  pendingDelete: WritableSignal<AdminHotelSummary | null>;
  requestToggle(hotel: AdminHotelSummary): void;
  confirmToggle(): void;
  requestDelete(hotel: AdminHotelSummary): void;
  confirmDelete(): void;
}

describe('AdminHotelList', () => {
  let fixture: ComponentFixture<AdminHotelList>;
  let component: AdminHotelList & AdminHotelListInternals;
  let serviceStub: {
    getHotels: ReturnType<typeof vi.fn>;
    deactivateHotel: ReturnType<typeof vi.fn>;
    reactivateHotel: ReturnType<typeof vi.fn>;
    deleteHotelPermanently: ReturnType<typeof vi.fn>;
  };

  const sampleHotel: AdminHotelSummary = {
    id: '1',
    name: 'Test Hotel',
    slug: 'test-hotel',
    city: 'Antalya',
    isActive: true,
    thumbnailUrl: null,
    updatedAtUtc: new Date().toISOString(),
  };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [AdminHotelList],
      providers: [provideRouter([]), { provide: AdminHotelsService, useValue: serviceStub }],
    });

    fixture = TestBed.createComponent(AdminHotelList);
    component = fixture.componentInstance as AdminHotelList & AdminHotelListInternals;
    fixture.detectChanges();
  }

  it('loads and displays hotels on init', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
      deleteHotelPermanently: vi.fn(),
    };
    createComponent();

    expect(component.hotels()).toEqual([sampleHotel]);
    expect(component.loading()).toBe(false);
    expect(component.error()).toBe(false);
  });

  it('shows the error state when the fetch fails', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(throwError(() => new Error('boom'))),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
      deleteHotelPermanently: vi.fn(),
    };
    createComponent();

    expect(component.error()).toBe(true);
    expect(component.loading()).toBe(false);
  });

  it('requestToggle stages a hotel for confirmation without calling the service', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deactivateHotel: vi.fn().mockReturnValue(of(undefined)),
      reactivateHotel: vi.fn().mockReturnValue(of(undefined)),
      deleteHotelPermanently: vi.fn(),
    };
    createComponent();

    component.requestToggle(sampleHotel);

    expect(component.pendingToggle()).toEqual(sampleHotel);
    expect(serviceStub.deactivateHotel).not.toHaveBeenCalled();
  });

  it('confirmToggle calls deactivateHotel for an active hotel, then clears the pending state', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deactivateHotel: vi.fn().mockReturnValue(of(undefined)),
      reactivateHotel: vi.fn().mockReturnValue(of(undefined)),
      deleteHotelPermanently: vi.fn(),
    };
    createComponent();

    component.requestToggle(sampleHotel);
    component.confirmToggle();

    expect(serviceStub.deactivateHotel).toHaveBeenCalledWith('1');
    expect(serviceStub.reactivateHotel).not.toHaveBeenCalled();
    expect(component.pendingToggle()).toBeNull();
  });

  it('confirmToggle calls reactivateHotel for an inactive hotel', () => {
    const inactiveHotel = { ...sampleHotel, isActive: false };
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [inactiveHotel], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deactivateHotel: vi.fn().mockReturnValue(of(undefined)),
      reactivateHotel: vi.fn().mockReturnValue(of(undefined)),
      deleteHotelPermanently: vi.fn(),
    };
    createComponent();

    component.requestToggle(inactiveHotel);
    component.confirmToggle();

    expect(serviceStub.reactivateHotel).toHaveBeenCalledWith('1');
    expect(serviceStub.deactivateHotel).not.toHaveBeenCalled();
  });

  it('requestDelete stages a hotel for confirmation without calling the service', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
      deleteHotelPermanently: vi.fn(),
    };
    createComponent();

    component.requestDelete(sampleHotel);

    expect(component.pendingDelete()).toEqual(sampleHotel);
    expect(serviceStub.deleteHotelPermanently).not.toHaveBeenCalled();
  });

  it('confirmDelete calls deleteHotelPermanently, then clears the pending state and refetches', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
      deleteHotelPermanently: vi.fn().mockReturnValue(of(undefined)),
    };
    createComponent();

    component.requestDelete(sampleHotel);
    component.confirmDelete();

    expect(serviceStub.deleteHotelPermanently).toHaveBeenCalledWith('1');
    expect(component.pendingDelete()).toBeNull();
    expect(serviceStub.getHotels).toHaveBeenCalledTimes(2);
  });
});
