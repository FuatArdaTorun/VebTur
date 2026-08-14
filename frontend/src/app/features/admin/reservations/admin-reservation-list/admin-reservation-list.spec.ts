import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AdminReservationList } from './admin-reservation-list';
import { AdminReservationsService } from '../admin-reservations.service';

describe('AdminReservationList', () => {
  let fixture: ComponentFixture<AdminReservationList>;
  let component: AdminReservationList;
  let serviceStub: { getReservations: ReturnType<typeof vi.fn>; deleteReservation?: ReturnType<typeof vi.fn> };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [AdminReservationList],
      providers: [provideRouter([]), { provide: AdminReservationsService, useValue: serviceStub }],
    });

    fixture = TestBed.createComponent(AdminReservationList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads and displays reservations on init', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [{ id: 'res-1' }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    expect(component['reservations']().length).toBe(1);
    expect(component['loading']()).toBe(false);
  });

  it('re-fetches with the selected status filter', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    component['statusControl'].setValue('Confirmed');
    component['applyFilter']();

    expect(serviceStub.getReservations).toHaveBeenLastCalledWith(expect.objectContaining({ status: 'Confirmed', page: 1 }));
  });

  it('shows the error state when the fetch fails', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) };
    createComponent();

    expect(component['error']()).toBe(true);
  });

  it('defaults to created-desc and sends it on the initial fetch', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    expect(serviceStub.getReservations).toHaveBeenCalledWith(expect.objectContaining({ sort: 'created-desc' }));
  });

  it('clicking a column header sorts ascending by that column and resets to page 1', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();
    component['page'].set(3);

    component['toggleSort']('hotel');

    expect(component['sort']()).toBe('hotel-asc');
    expect(component['sortIndicator']('hotel')).toBe('▲');
    expect(serviceStub.getReservations).toHaveBeenLastCalledWith(expect.objectContaining({ sort: 'hotel-asc', page: 1 }));
  });

  it('clicking the same column header again toggles to descending', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    component['toggleSort']('checkin');
    component['toggleSort']('checkin');

    expect(component['sort']()).toBe('checkin-desc');
    expect(component['sortIndicator']('checkin')).toBe('▼');
  });

  it('switching to a different column starts ascending again', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    component['toggleSort']('status');
    component['toggleSort']('status');
    component['toggleSort']('hotel');

    expect(component['sort']()).toBe('hotel-asc');
    expect(component['sortIndicator']('status')).toBe('');
  });

  it('starts with an empty search box and no search filter on the initial fetch', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    expect(component['searchControl'].value).toBe('');
    expect(serviceStub.getReservations).toHaveBeenCalledWith(expect.objectContaining({ search: undefined }));
  });

  it('applying the filter re-fetches with whatever was typed (reference, guest, or hotel) and resets to page 1', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();
    component['page'].set(3);

    component['searchControl'].setValue('jane@example.com');
    component['applyFilter']();

    expect(serviceStub.getReservations).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'jane@example.com', page: 1 }));
  });

  it('requesting delete opens a confirmation for that reservation', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    const reservation = { id: 'res-1', referenceNumber: 'VEB-4F7K9QRT' } as never;
    component['requestDelete'](reservation);

    expect(component['pendingDelete']()).toBe(reservation);
  });

  it('confirming delete calls the service, clears the pending state, and refetches', () => {
    serviceStub = {
      getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })),
      deleteReservation: vi.fn().mockReturnValue(of(undefined)),
    };
    createComponent();

    const reservation = { id: 'res-1', referenceNumber: 'VEB-4F7K9QRT' } as never;
    component['requestDelete'](reservation);
    component['confirmDelete']();

    expect(serviceStub.deleteReservation).toHaveBeenCalledWith('res-1');
    expect(component['pendingDelete']()).toBeNull();
    expect(serviceStub.getReservations).toHaveBeenCalledTimes(2);
  });
});
