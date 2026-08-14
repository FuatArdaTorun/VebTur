import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { MyReservationList } from './my-reservation-list';
import { ReservationsService } from '../reservations.service';

describe('MyReservationList', () => {
  let fixture: ComponentFixture<MyReservationList>;
  let component: MyReservationList;
  let serviceStub: { getMine: ReturnType<typeof vi.fn> };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [MyReservationList],
      providers: [provideRouter([]), { provide: ReservationsService, useValue: serviceStub }],
    });

    fixture = TestBed.createComponent(MyReservationList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads and displays the reservations on init', () => {
    serviceStub = { getMine: vi.fn().mockReturnValue(of({ items: [{ id: 'res-1' }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    expect(component['reservations']().length).toBe(1);
    expect(component['loading']()).toBe(false);
    expect(component['error']()).toBe(false);
  });

  it('shows the error state when the fetch fails', () => {
    serviceStub = { getMine: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) };
    createComponent();

    expect(component['error']()).toBe(true);
    expect(component['loading']()).toBe(false);
  });
});
