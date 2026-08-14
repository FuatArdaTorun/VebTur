import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { AdminReservationDetail } from './admin-reservation-detail';
import { AdminReservationsService } from '../admin-reservations.service';
import { AdminReservationDetail as AdminReservationDetailModel } from '../models/admin-reservation.model';
import { ReservationStatus } from '../../../reservations/models/reservation.model';

function buildReservation(status: ReservationStatus): AdminReservationDetailModel {
  return {
    id: 'res-1',
    referenceNumber: 'VEB-ABC12345',
    hotelId: 'hotel-1',
    hotelName: 'Test Hotel',
    roomTypeId: 'room-1',
    roomTypeName: 'Standard',
    roomTypeAvailableCount: 0,
    guestFullName: 'Jane Guest',
    guestEmail: 'jane@example.com',
    guestPhone: '+90 555 000 00 00',
    checkInDate: '2026-09-01',
    checkOutDate: '2026-09-04',
    adultCount: 2,
    childCount: 0,
    specialRequests: null,
    estimatedPrice: 3000,
    currency: 'TRY',
    status,
    createdAtUtc: new Date().toISOString(),
    updatedAtUtc: new Date().toISOString(),
  };
}

describe('AdminReservationDetail', () => {
  let fixture: ComponentFixture<AdminReservationDetail>;
  let component: AdminReservationDetail;
  let serviceStub: {
    getReservation: ReturnType<typeof vi.fn>;
    confirm: ReturnType<typeof vi.fn>;
    reject: ReturnType<typeof vi.fn>;
    cancel: ReturnType<typeof vi.fn>;
  };

  function createComponent(status: ReservationStatus): void {
    serviceStub = {
      getReservation: vi.fn().mockReturnValue(of(buildReservation(status))),
      confirm: vi.fn(),
      reject: vi.fn(),
      cancel: vi.fn(),
    };

    TestBed.configureTestingModule({
      imports: [AdminReservationDetail],
      providers: [
        provideRouter([]),
        { provide: AdminReservationsService, useValue: serviceStub },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'res-1' }) } } },
      ],
    });

    fixture = TestBed.createComponent(AdminReservationDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it.each<ReservationStatus>(['Pending', 'Sent'])('allows confirm/reject for a %s reservation', (status) => {
    createComponent(status);

    expect(component['canActOn']()).toBe(true);
  });

  it.each<ReservationStatus>(['Confirmed', 'Rejected', 'Cancelled'])('does not allow confirm/reject for a %s reservation', (status) => {
    createComponent(status);

    expect(component['canActOn']()).toBe(false);
  });

  it('confirm calls the service and refetches on success', () => {
    createComponent('Sent');
    serviceStub.confirm.mockReturnValue(of(undefined));

    component['confirm']();

    expect(serviceStub.confirm).toHaveBeenCalledWith('res-1');
    expect(serviceStub.getReservation).toHaveBeenCalledTimes(2);
  });

  it('surfaces the "no rooms available" error from a failed confirm without refetching', () => {
    createComponent('Sent');
    const httpError = new HttpErrorResponse({ status: 400, error: { errors: { AvailableCount: ['No rooms available for this room type.'] } } });
    serviceStub.confirm.mockReturnValue(throwError(() => httpError));

    component['confirm']();

    expect(component['actionError']()).toBe('No rooms available for this room type.');
    expect(serviceStub.getReservation).toHaveBeenCalledTimes(1);
  });

  it('cancel calls the service, closes the confirm dialog, and refetches', () => {
    createComponent('Confirmed');
    serviceStub.cancel.mockReturnValue(of(undefined));

    component['confirmCancel']();

    expect(serviceStub.cancel).toHaveBeenCalledWith('res-1');
    expect(component['pendingCancel']()).toBe(false);
    expect(serviceStub.getReservation).toHaveBeenCalledTimes(2);
  });
});
