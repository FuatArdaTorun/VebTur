import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { MyReservationDetail } from './my-reservation-detail';
import { ReservationsService } from '../reservations.service';
import { ReservationRequestDetail, ReservationStatus } from '../models/reservation.model';

function buildReservation(status: ReservationStatus): ReservationRequestDetail {
  return {
    id: 'res-1',
    referenceNumber: 'VEB-ABC12345',
    hotelId: 'hotel-1',
    hotelName: 'Test Hotel',
    roomTypeId: 'room-1',
    roomTypeName: 'Standard',
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

describe('MyReservationDetail', () => {
  let fixture: ComponentFixture<MyReservationDetail>;
  let component: MyReservationDetail;
  let serviceStub: { getMineById: ReturnType<typeof vi.fn>; cancelMine: ReturnType<typeof vi.fn> };

  function createComponent(status: ReservationStatus): void {
    serviceStub = {
      getMineById: vi.fn().mockReturnValue(of(buildReservation(status))),
      cancelMine: vi.fn().mockReturnValue(of(undefined)),
    };

    TestBed.configureTestingModule({
      imports: [MyReservationDetail],
      providers: [
        provideRouter([]),
        { provide: ReservationsService, useValue: serviceStub },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'res-1' }) } } },
      ],
    });

    fixture = TestBed.createComponent(MyReservationDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it.each<ReservationStatus>(['Pending', 'Sent', 'Confirmed'])('shows the actions bar for a %s reservation', (status) => {
    createComponent(status);

    expect(component['canManage']()).toBe(true);
  });

  it.each<ReservationStatus>(['Cancelled', 'Rejected'])('hides the actions bar for a %s reservation', (status) => {
    createComponent(status);

    expect(component['canManage']()).toBe(false);
  });

  it('shows the Edit link for a Pending reservation', () => {
    createComponent('Pending');
    expect(fixture.nativeElement.querySelector('a[href*="/edit"]')).not.toBeNull();
  });

  it('hides the Edit link for a Confirmed reservation', () => {
    createComponent('Confirmed');
    expect(fixture.nativeElement.querySelector('a[href*="/edit"]')).toBeNull();
  });

  it.each<ReservationStatus>(['Pending', 'Sent'])('shows the "not confirmed" notice while a %s reservation awaits a decision', (status) => {
    createComponent(status);

    expect(component['isAwaitingDecision']()).toBe(true);
  });

  it.each<ReservationStatus>(['Confirmed', 'Rejected', 'Cancelled'])('hides the "not confirmed" notice once a %s reservation has a final decision', (status) => {
    createComponent(status);

    expect(component['isAwaitingDecision']()).toBe(false);
  });

  it('cancels the reservation and refetches it', () => {
    createComponent('Confirmed');

    component['confirmCancel']();

    expect(serviceStub.cancelMine).toHaveBeenCalledWith('res-1');
    expect(serviceStub.getMineById).toHaveBeenCalledTimes(2);
    expect(component['confirmingCancel']()).toBe(false);
  });
});
