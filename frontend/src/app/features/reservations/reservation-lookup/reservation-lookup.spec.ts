import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { ReservationLookup } from './reservation-lookup';
import { ReservationsService } from '../reservations.service';

describe('ReservationLookup', () => {
  let fixture: ComponentFixture<ReservationLookup>;
  let component: ReservationLookup;
  let serviceStub: { getByReference: ReturnType<typeof vi.fn> };
  let router: Router;

  function createComponent(reference: string | null): void {
    TestBed.configureTestingModule({
      imports: [ReservationLookup],
      providers: [
        provideRouter([]),
        { provide: ReservationsService, useValue: serviceStub },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(reference ? { reference } : {}) } } },
      ],
    });

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(ReservationLookup);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('does not look anything up when no reference is in the route', () => {
    serviceStub = { getByReference: vi.fn() };
    createComponent(null);

    expect(serviceStub.getByReference).not.toHaveBeenCalled();
    expect(component['reservation']()).toBeNull();
  });

  it('looks up and displays the reservation when a reference is in the route', () => {
    serviceStub = { getByReference: vi.fn().mockReturnValue(of({ id: 'res-1', referenceNumber: 'VEB-ABC12345' })) };
    createComponent('VEB-ABC12345');

    expect(serviceStub.getByReference).toHaveBeenCalledWith('VEB-ABC12345');
    expect(component['reservation']()?.id).toBe('res-1');
  });

  it('shows the "not confirmed" notice while the reservation is AwaitingApproval', () => {
    serviceStub = { getByReference: vi.fn().mockReturnValue(of({ id: 'res-1', referenceNumber: 'VEB-ABC12345', status: 'AwaitingApproval' })) };
    createComponent('VEB-ABC12345');

    expect(component['isAwaitingDecision']()).toBe(true);
  });

  it.each(['Confirmed', 'Rejected', 'Cancelled'])('hides the "not confirmed" notice once the reservation is %s', (status) => {
    serviceStub = { getByReference: vi.fn().mockReturnValue(of({ id: 'res-1', referenceNumber: 'VEB-ABC12345', status })) };
    createComponent('VEB-ABC12345');

    expect(component['isAwaitingDecision']()).toBe(false);
  });

  it('shows an error when the reference is not found', () => {
    serviceStub = { getByReference: vi.fn().mockReturnValue(throwError(() => new Error('not found'))) };
    createComponent('VEB-DOESNOTEXIST');

    expect(component['error']()).toBe(true);
  });

  it('submitting the lookup form navigates to the reference-scoped route', () => {
    serviceStub = { getByReference: vi.fn() };
    createComponent(null);

    component['form'].setValue({ reference: 'VEB-ABC12345' });
    component['submit']();

    expect(router.navigate).toHaveBeenCalledWith(['/reservations/lookup', 'VEB-ABC12345']);
  });
});
