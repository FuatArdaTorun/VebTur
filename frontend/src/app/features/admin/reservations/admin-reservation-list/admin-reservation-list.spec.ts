import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { AdminReservationList } from './admin-reservation-list';
import { AdminReservationsService } from '../admin-reservations.service';
import { ReservationStatus } from '../../../reservations/models/reservation.model';

describe('AdminReservationList', () => {
  let fixture: ComponentFixture<AdminReservationList>;
  let component: AdminReservationList;
  let serviceStub: {
    getReservations: ReturnType<typeof vi.fn>;
    deleteReservations?: ReturnType<typeof vi.fn>;
  };

  function createComponent(queryParams: Record<string, string> = {}): void {
    TestBed.configureTestingModule({
      imports: [AdminReservationList],
      providers: [
        provideRouter([]),
        { provide: AdminReservationsService, useValue: serviceStub },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } } },
      ],
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

  it('defaults to status-asc (AwaitingApproval first) and sends it on the initial fetch', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    expect(serviceStub.getReservations).toHaveBeenCalledWith(expect.objectContaining({ sort: 'status-asc' }));
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

  it('does not allow delete for an AwaitingApproval reservation (must be confirmed/rejected first)', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    const reservation = { id: 'res-1', status: 'AwaitingApproval' as ReservationStatus } as never;
    expect(component['canDelete'](reservation)).toBe(false);
  });

  it.each<ReservationStatus>(['Confirmed', 'Rejected', 'Cancelled'])('allows delete for a %s reservation', (status) => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    const reservation = { id: 'res-1', status } as never;
    expect(component['canDelete'](reservation)).toBe(true);
  });

  it('selection mode is off by default, with no checkboxes and no single Delete button hidden', () => {
    serviceStub = {
      getReservations: vi.fn().mockReturnValue(of({ items: [{ id: 'res-1', status: 'Confirmed' }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
    };
    createComponent();

    expect(component['selectionMode']()).toBe(false);
    expect(fixture.nativeElement.querySelector('input[type="checkbox"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Delete Reservation');
    expect(fixture.nativeElement.textContent).toContain('Delete');
  });

  it('toggling selection mode on shows checkboxes only for deletable rows; toggling it off clears any selection', () => {
    serviceStub = {
      getReservations: vi.fn().mockReturnValue(
        of({
          items: [
            { id: 'res-1', status: 'Confirmed' },
            { id: 'res-2', status: 'AwaitingApproval' },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 2,
          totalPages: 1,
        }),
      ),
    };
    createComponent();

    component['toggleSelectionMode']();
    fixture.detectChanges();

    expect(component['selectionMode']()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('tbody input[type="checkbox"]').length).toBe(1);

    component['toggleSelect']('res-1');
    expect(component['selectedCount']()).toBe(1);

    component['toggleSelectionMode']();
    expect(component['selectionMode']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
  });

  it('select-all only selects deletable (non-AwaitingApproval) rows', () => {
    serviceStub = {
      getReservations: vi.fn().mockReturnValue(
        of({
          items: [
            { id: 'res-1', status: 'Confirmed' },
            { id: 'res-2', status: 'AwaitingApproval' },
            { id: 'res-3', status: 'Rejected' },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 3,
          totalPages: 1,
        }),
      ),
    };
    createComponent();
    component['toggleSelectionMode']();

    component['toggleSelectAll']();

    expect(component['isAllSelected']()).toBe(true);
    expect(component['selectedCount']()).toBe(2);
    expect(component['isSelected']('res-2')).toBe(false);

    component['toggleSelectAll']();
    expect(component['selectedCount']()).toBe(0);
  });

  it('requestBulkDelete opens the confirm dialog only when something is selected', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [{ id: 'res-1', status: 'Confirmed' }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();
    component['toggleSelectionMode']();

    component['requestBulkDelete']();
    expect(component['confirmingBulkDelete']()).toBe(false);

    component['toggleSelect']('res-1');
    component['requestBulkDelete']();
    expect(component['confirmingBulkDelete']()).toBe(true);
  });

  it('confirming bulk delete calls the service with the selected ids, exits selection mode, and refetches', () => {
    serviceStub = {
      getReservations: vi.fn().mockReturnValue(of({ items: [{ id: 'res-1', status: 'Confirmed' }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deleteReservations: vi.fn().mockReturnValue(of(undefined)),
    };
    createComponent();
    component['toggleSelectionMode']();
    component['toggleSelect']('res-1');
    component['requestBulkDelete']();

    component['confirmBulkDelete']();

    expect(serviceStub.deleteReservations).toHaveBeenCalledWith(['res-1']);
    expect(component['confirmingBulkDelete']()).toBe(false);
    expect(component['selectionMode']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
    expect(serviceStub.getReservations).toHaveBeenCalledTimes(2);
  });

  it('surfaces the error from a rejected bulk delete without refetching', () => {
    serviceStub = {
      getReservations: vi.fn().mockReturnValue(of({ items: [{ id: 'res-1', status: 'Confirmed' }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })),
      deleteReservations: vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400, error: { errors: { Ids: ['Select at least one reservation to delete.'] } } }))),
    };
    createComponent();
    component['toggleSelectionMode']();
    component['toggleSelect']('res-1');
    component['requestBulkDelete']();

    component['confirmBulkDelete']();

    expect(component['deleteError']()).toBe('Select at least one reservation to delete.');
    expect(component['confirmingBulkDelete']()).toBe(false);
    expect(serviceStub.getReservations).toHaveBeenCalledTimes(1);
  });

  it('pre-fills the search box from a "search" route query param and sends it on the initial fetch', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent({ search: 'Cancel Restriction Test Hotel' });

    expect(component['searchControl'].value).toBe('Cancel Restriction Test Hotel');
    expect(serviceStub.getReservations).toHaveBeenCalledWith(expect.objectContaining({ search: 'Cancel Restriction Test Hotel' }));
  });

  it('leaves the search box empty when arriving without a "search" query param', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    expect(component['searchControl'].value).toBe('');
    expect(serviceStub.getReservations).toHaveBeenCalledWith(expect.objectContaining({ search: undefined }));
  });

  it('clearing the pre-filled search box and re-applying removes the filter, same as any other search edit', () => {
    serviceStub = { getReservations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent({ search: 'Cancel Restriction Test Hotel' });

    component['searchControl'].setValue('');
    component['applyFilter']();

    expect(serviceStub.getReservations).toHaveBeenLastCalledWith(expect.objectContaining({ search: undefined }));
  });
});
