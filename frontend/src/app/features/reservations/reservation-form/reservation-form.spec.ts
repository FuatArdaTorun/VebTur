import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { ReservationForm } from './reservation-form';
import { HotelsService } from '../../hotels/hotels.service';
import { ReservationsService } from '../reservations.service';
import { AuthService } from '../../../core/auth/auth.service';
import { HotelDetail, RoomType } from '../../hotels/models/hotel.model';
import { ReservationRequestDetail } from '../models/reservation.model';

const roomTypes: RoomType[] = [
  { id: 'room-1', name: 'Standard', description: 'd', capacity: 2, baseNightlyPrice: 1000, currency: 'TRY', availableCount: 3 },
  { id: 'room-2', name: 'Suite', description: 'd', capacity: 4, baseNightlyPrice: 2000, currency: 'TRY', availableCount: 1 },
];

const sampleHotel: HotelDetail = {
  id: 'hotel-1',
  name: 'Test Hotel',
  slug: 'test-hotel',
  description: 'd',
  city: 'Antalya',
  country: 'Turkey',
  address: 'Addr',
  latitude: 0,
  longitude: 0,
  starRating: null,
  googleRating: null,
  googleRatingCount: null,
  officialWebsiteUrl: null,
  phoneNumber: null,
  images: [],
  amenities: [],
  roomTypes,
  customerRating: null,
  customerReviewCount: 0,
};

const sampleReservation: ReservationRequestDetail = {
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
  status: 'Confirmed',
  createdAtUtc: new Date().toISOString(),
  updatedAtUtc: new Date().toISOString(),
};

describe('ReservationForm', () => {
  let fixture: ComponentFixture<ReservationForm>;
  let component: ReservationForm;
  let hotelsServiceStub: { getHotel: ReturnType<typeof vi.fn>; getRoomTypeAvailability: ReturnType<typeof vi.fn> };
  let reservationsServiceStub: { getMineById: ReturnType<typeof vi.fn>; create: ReturnType<typeof vi.fn>; updateMine: ReturnType<typeof vi.fn> };
  let authServiceStub: { currentUser: ReturnType<typeof vi.fn>; isAuthenticated: ReturnType<typeof vi.fn>; getProfile: ReturnType<typeof vi.fn> };
  let router: Router;

  function createComponent(options: { editId?: string; queryParams?: Record<string, string> } = {}): void {
    TestBed.configureTestingModule({
      imports: [ReservationForm],
      providers: [
        provideRouter([]),
        { provide: HotelsService, useValue: hotelsServiceStub },
        { provide: ReservationsService, useValue: reservationsServiceStub },
        { provide: AuthService, useValue: authServiceStub },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(options.editId ? { id: options.editId } : {}),
              queryParamMap: convertToParamMap(options.queryParams ?? {}),
            },
          },
        },
      ],
    });

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(ReservationForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    authServiceStub = {
      currentUser: vi.fn().mockReturnValue(null),
      isAuthenticated: vi.fn().mockReturnValue(false),
      getProfile: vi.fn(),
    };
  });

  describe('create mode', () => {
    beforeEach(() => {
      hotelsServiceStub = {
        getHotel: vi.fn().mockReturnValue(of(sampleHotel)),
        getRoomTypeAvailability: vi.fn().mockReturnValue(of({ fullyBookedDates: [] })),
      };
      reservationsServiceStub = { getMineById: vi.fn(), create: vi.fn(), updateMine: vi.fn() };
    });

    it('loads the hotel and room types, defaulting to the roomTypeId from the query params', () => {
      createComponent({ queryParams: { hotelId: 'hotel-1', roomTypeId: 'room-2' } });

      expect(component['isEditMode']()).toBe(false);
      expect(component['hotelName']()).toBe('Test Hotel');
      expect(component['roomTypes']()).toEqual(roomTypes);
      expect(component['form'].controls.roomTypeId.value).toBe('room-2');
    });

    it('fetches booked dates for the initially selected room type', () => {
      createComponent({ queryParams: { hotelId: 'hotel-1', roomTypeId: 'room-2' } });

      expect(hotelsServiceStub.getRoomTypeAvailability).toHaveBeenCalledWith('room-2');
    });

    it('refetches booked dates when the room type selection changes', () => {
      hotelsServiceStub.getRoomTypeAvailability.mockReturnValue(of({ fullyBookedDates: ['2026-09-05'] }));
      createComponent({ queryParams: { hotelId: 'hotel-1', roomTypeId: 'room-1' } });

      component['form'].controls.roomTypeId.setValue('room-2');
      component['onRoomTypeChange']();

      expect(hotelsServiceStub.getRoomTypeAvailability).toHaveBeenCalledWith('room-2');
      expect(component['bookedDates']()).toEqual(['2026-09-05']);
    });

    it('does not show the "use my info" checkbox for a signed-out guest', () => {
      createComponent({ queryParams: { hotelId: 'hotel-1' } });

      expect(component['isAuthenticated']()).toBe(false);
    });

    it('fills guest name/email/phone from the profile when "use my info" is checked', () => {
      authServiceStub.isAuthenticated.mockReturnValue(true);
      authServiceStub.getProfile.mockReturnValue(
        of({ id: 'u1', email: 'a@b.com', displayName: 'A B', phoneNumber: '+90 555 111 22 33', roles: ['Customer'] }),
      );

      createComponent({ queryParams: { hotelId: 'hotel-1' } });
      component['onUseMyInfoChange'](true);

      expect(component['form'].controls.guestFullName.value).toBe('A B');
      expect(component['form'].controls.guestEmail.value).toBe('a@b.com');
      expect(component['form'].controls.guestPhone.value).toBe('+90 555 111 22 33');
    });

    it('clears the guest fields when "use my info" is unchecked', () => {
      createComponent({ queryParams: { hotelId: 'hotel-1' } });
      component['form'].patchValue({ guestFullName: 'Someone', guestEmail: 'x@y.com', guestPhone: '123' });

      component['onUseMyInfoChange'](false);

      expect(component['form'].controls.guestFullName.value).toBe('');
      expect(component['form'].controls.guestEmail.value).toBe('');
      expect(component['form'].controls.guestPhone.value).toBe('');
    });

    it('computes the estimated price as nights times the selected room rate', () => {
      createComponent({ queryParams: { hotelId: 'hotel-1', roomTypeId: 'room-1' } });

      component['form'].patchValue({ checkInDate: '2026-09-01', checkOutDate: '2026-09-04' });

      expect(component['estimatedPrice']()).toBe(3000);
    });

    it('does not call create when the form is invalid', () => {
      createComponent({ queryParams: { hotelId: 'hotel-1' } });

      component['submit']();

      expect(reservationsServiceStub.create).not.toHaveBeenCalled();
    });

    it('creates the reservation and navigates to the success page with the reference number', () => {
      reservationsServiceStub.create.mockReturnValue(of({ ...sampleReservation, referenceNumber: 'VEB-XYZ98765' }));
      createComponent({ queryParams: { hotelId: 'hotel-1', roomTypeId: 'room-1' } });

      component['form'].patchValue({
        guestFullName: 'Jane',
        guestEmail: 'jane@example.com',
        guestPhone: '+90 555 000 00 00',
        checkInDate: '2026-09-01',
        checkOutDate: '2026-09-04',
      });
      component['submit']();

      expect(reservationsServiceStub.create).toHaveBeenCalledWith(expect.objectContaining({ hotelId: 'hotel-1', roomTypeId: 'room-1' }));
      expect(router.navigate).toHaveBeenCalledWith(['/reservations/success', 'VEB-XYZ98765']);
    });

    it('surfaces a server validation error without navigating', () => {
      const httpError = new HttpErrorResponse({ status: 400, error: { errors: { AdultCount: ['This room type accommodates at most 2 guests.'] } } });
      reservationsServiceStub.create.mockReturnValue(throwError(() => httpError));
      createComponent({ queryParams: { hotelId: 'hotel-1', roomTypeId: 'room-1' } });

      component['form'].patchValue({
        guestFullName: 'Jane',
        guestEmail: 'jane@example.com',
        guestPhone: '+90 555 000 00 00',
        checkInDate: '2026-09-01',
        checkOutDate: '2026-09-04',
      });
      component['submit']();

      expect(component['submitError']()).toBe('This room type accommodates at most 2 guests.');
      expect(router.navigate).not.toHaveBeenCalled();
    });
  });

  describe('edit mode', () => {
    beforeEach(() => {
      hotelsServiceStub = {
        getHotel: vi.fn().mockReturnValue(of(sampleHotel)),
        getRoomTypeAvailability: vi.fn().mockReturnValue(of({ fullyBookedDates: [] })),
      };
      reservationsServiceStub = {
        getMineById: vi.fn().mockReturnValue(of(sampleReservation)),
        create: vi.fn(),
        updateMine: vi.fn().mockReturnValue(of(sampleReservation)),
      };
    });

    it('loads the existing reservation and disables the guest identity fields', () => {
      createComponent({ editId: 'res-1' });

      expect(component['isEditMode']()).toBe(true);
      expect(component['form'].controls.roomTypeId.value).toBe('room-1');
      expect(component['form'].controls.guestFullName.disabled).toBe(true);
      expect(component['form'].controls.guestEmail.disabled).toBe(true);
      expect(component['form'].controls.guestPhone.disabled).toBe(true);
    });

    it('updates the reservation and navigates to its detail page', () => {
      createComponent({ editId: 'res-1' });

      component['submit']();

      expect(reservationsServiceStub.updateMine).toHaveBeenCalledWith(
        'res-1',
        expect.objectContaining({ roomTypeId: 'room-1', checkInDate: '2026-09-01', checkOutDate: '2026-09-04' }),
      );
      expect(router.navigate).toHaveBeenCalledWith(['/my-reservations', sampleReservation.id]);
    });
  });
});
