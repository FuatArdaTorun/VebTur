import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { WritableSignal } from '@angular/core';
import { of } from 'rxjs';

import { AdminHotelList } from './admin-hotel-list';
import { AdminHotelsService } from '../admin-hotels.service';
import { AdminHotelSort, AdminHotelSummary } from '../models/admin-hotel.model';

interface AdminHotelListInternals {
  hotels: WritableSignal<AdminHotelSummary[]>;
  loading: WritableSignal<boolean>;
  error: WritableSignal<boolean>;
  pendingToggle: WritableSignal<AdminHotelSummary | null>;
  selectionMode: WritableSignal<boolean>;
  selectedIds: WritableSignal<Set<string>>;
  confirmingBulkDelete: WritableSignal<boolean>;
  page: WritableSignal<number>;
  sort: WritableSignal<AdminHotelSort>;
  selectedCount(): number;
  isAllSelected(): boolean;
  canDelete(hotel: AdminHotelSummary): boolean;
  requestToggle(hotel: AdminHotelSummary): void;
  confirmToggle(): void;
  toggleSelectionMode(): void;
  isSelected(id: string): boolean;
  toggleSelect(id: string): void;
  toggleSelectAll(): void;
  requestBulkDelete(): void;
  confirmBulkDelete(): void;
  toggleSort(column: 'name' | 'city' | 'status' | 'updated'): void;
  sortIndicator(column: 'name' | 'city' | 'status' | 'updated'): string;
}

describe('AdminHotelList', () => {
  let fixture: ComponentFixture<AdminHotelList>;
  let component: AdminHotelList & AdminHotelListInternals;
  let serviceStub: {
    getHotels: ReturnType<typeof vi.fn>;
    deactivateHotel: ReturnType<typeof vi.fn>;
    reactivateHotel: ReturnType<typeof vi.fn>;
    deleteHotelsPermanently?: ReturnType<typeof vi.fn>;
  };

  const sampleHotel: AdminHotelSummary = {
    id: '1',
    name: 'Test Hotel',
    slug: 'test-hotel',
    city: 'Antalya',
    isActive: true,
    thumbnailUrl: null,
    updatedAtUtc: new Date().toISOString(),
    hasReservationHistory: false,
  };

  const blockedHotel: AdminHotelSummary = { ...sampleHotel, id: '2', name: 'Blocked Hotel', hasReservationHistory: true };

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
    };
    createComponent();

    expect(component.hotels()).toEqual([sampleHotel]);
    expect(component.loading()).toBe(false);
    expect(component.error()).toBe(false);
  });

  it('requestToggle stages a hotel for confirmation without calling the service', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deactivateHotel: vi.fn().mockReturnValue(of(undefined)),
      reactivateHotel: vi.fn().mockReturnValue(of(undefined)),
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
    };
    createComponent();

    component.requestToggle(sampleHotel);
    component.confirmToggle();

    expect(serviceStub.deactivateHotel).toHaveBeenCalledWith('1');
    expect(serviceStub.reactivateHotel).not.toHaveBeenCalled();
    expect(component.pendingToggle()).toBeNull();
  });

  it('does not allow delete for a hotel with reservation history', () => {
    serviceStub = { getHotels: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })), deactivateHotel: vi.fn(), reactivateHotel: vi.fn() };
    createComponent();

    expect(component.canDelete(blockedHotel)).toBe(false);
    expect(component.canDelete(sampleHotel)).toBe(true);
  });

  it('toggling selection mode on shows checkboxes only for deletable hotels; a blocked hotel shows a hint link instead', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel, blockedHotel], page: 1, pageSize: 20, totalCount: 2, totalPages: 1 })),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
    };
    createComponent();

    component.toggleSelectionMode();
    fixture.detectChanges();

    expect(component.selectionMode()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('tbody input[type="checkbox"]').length).toBe(1);
    const hint: HTMLAnchorElement = fixture.nativeElement.querySelector('.admin-hotel-list__blocked-hint');
    expect(hint).not.toBeNull();
    expect(hint.getAttribute('href')).toBe('/admin/reservations?search=Blocked%20Hotel');
  });

  it('select-all only selects deletable hotels', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel, blockedHotel], page: 1, pageSize: 20, totalCount: 2, totalPages: 1 })),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
    };
    createComponent();
    component.toggleSelectionMode();

    component.toggleSelectAll();

    expect(component.isAllSelected()).toBe(true);
    expect(component.selectedCount()).toBe(1);
    expect(component.isSelected(blockedHotel.id)).toBe(false);
  });

  it('requestBulkDelete opens the confirm dialog only when something is selected', () => {
    serviceStub = { getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })), deactivateHotel: vi.fn(), reactivateHotel: vi.fn() };
    createComponent();
    component.toggleSelectionMode();

    component.requestBulkDelete();
    expect(component.confirmingBulkDelete()).toBe(false);

    component.toggleSelect(sampleHotel.id);
    component.requestBulkDelete();
    expect(component.confirmingBulkDelete()).toBe(true);
  });

  it('confirming bulk delete calls the service with the selected ids, exits selection mode, and refetches', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [sampleHotel], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
      deleteHotelsPermanently: vi.fn().mockReturnValue(of(undefined)),
    };
    createComponent();
    component.toggleSelectionMode();
    component.toggleSelect(sampleHotel.id);
    component.requestBulkDelete();

    component.confirmBulkDelete();

    expect(serviceStub.deleteHotelsPermanently).toHaveBeenCalledWith(['1']);
    expect(component.confirmingBulkDelete()).toBe(false);
    expect(component.selectionMode()).toBe(false);
    expect(component.selectedCount()).toBe(0);
    expect(serviceStub.getHotels).toHaveBeenCalledTimes(2);
  });

  it('clicking a column header sorts ascending by that column and resets to page 1', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
    };
    createComponent();
    component.page.set(3);

    component.toggleSort('name');

    expect(component.sort()).toBe('name-asc');
    expect(component.sortIndicator('name')).toBe('▲');
    expect(serviceStub.getHotels).toHaveBeenLastCalledWith(expect.objectContaining({ sort: 'name-asc', page: 1 }));
  });

  it('clicking the same column header again toggles to descending', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
    };
    createComponent();

    component.toggleSort('city');
    component.toggleSort('city');

    expect(component.sort()).toBe('city-desc');
    expect(component.sortIndicator('city')).toBe('▼');
  });

  it('switching to a different column starts ascending again', () => {
    serviceStub = {
      getHotels: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })),
      deactivateHotel: vi.fn(),
      reactivateHotel: vi.fn(),
    };
    createComponent();

    component.toggleSort('status');
    component.toggleSort('status');
    component.toggleSort('name');

    expect(component.sort()).toBe('name-asc');
    expect(component.sortIndicator('status')).toBe('');
  });
});
