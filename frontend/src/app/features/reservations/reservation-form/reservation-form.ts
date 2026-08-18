import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { HotelsService } from '../../hotels/hotels.service';
import { RoomType } from '../../hotels/models/hotel.model';
import { AuthService } from '../../../core/auth/auth.service';
import { ReservationsService } from '../reservations.service';
import { CreateReservationRequest, UpdateReservationRequest } from '../models/reservation.model';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../shared/error-state/error-state';

@Component({
  selector: 'app-reservation-form',
  imports: [ReactiveFormsModule, LoadingState, ErrorState],
  templateUrl: './reservation-form.html',
  styleUrl: './reservation-form.scss',
})
export class ReservationForm {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly hotelsService = inject(HotelsService);
  private readonly reservationsService = inject(ReservationsService);
  private readonly authService = inject(AuthService);

  protected readonly isEditMode = signal(false);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly hotelName = signal('');
  protected readonly roomTypes = signal<RoomType[]>([]);
  protected readonly useMyInfo = signal(false);

  protected readonly isAuthenticated = this.authService.isAuthenticated;

  private reservationId: string | null = null;
  private hotelId = '';

  protected readonly form = new FormGroup({
    guestFullName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    guestEmail: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    guestPhone: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    roomTypeId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    checkInDate: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    checkOutDate: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    adultCount: new FormControl(2, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    childCount: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    specialRequests: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    const editId = this.route.snapshot.paramMap.get('id');
    if (editId) {
      this.isEditMode.set(true);
      this.reservationId = editId;
      this.loadForEdit(editId);
    } else {
      this.loadForCreate();
    }
  }

  protected estimatedPrice(): number {
    const room = this.roomTypes().find((r) => r.id === this.form.controls.roomTypeId.value);
    const checkIn = this.form.controls.checkInDate.value;
    const checkOut = this.form.controls.checkOutDate.value;
    if (!room || !checkIn || !checkOut) {
      return 0;
    }

    const nights = (new Date(checkOut).getTime() - new Date(checkIn).getTime()) / (1000 * 60 * 60 * 24);
    return nights > 0 ? nights * room.baseNightlyPrice : 0;
  }

  protected selectedCurrency(): string {
    return this.roomTypes().find((r) => r.id === this.form.controls.roomTypeId.value)?.currency ?? '';
  }

  protected onUseMyInfoChange(checked: boolean): void {
    this.useMyInfo.set(checked);

    if (!checked) {
      this.form.patchValue({ guestFullName: '', guestEmail: '', guestPhone: '' });
      return;
    }

    this.authService.getProfile().subscribe((profile) => {
      this.form.patchValue({
        guestFullName: profile.displayName,
        guestEmail: profile.email,
        guestPhone: profile.phoneNumber ?? '',
      });
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);
    const value = this.form.getRawValue();

    if (this.isEditMode()) {
      const dto: UpdateReservationRequest = {
        roomTypeId: value.roomTypeId,
        checkInDate: value.checkInDate,
        checkOutDate: value.checkOutDate,
        adultCount: value.adultCount,
        childCount: value.childCount,
        specialRequests: value.specialRequests || null,
      };

      this.reservationsService.updateMine(this.reservationId!, dto).subscribe({
        next: (updated) => this.router.navigate(['/my-reservations', updated.id]),
        error: (response: HttpErrorResponse) => {
          this.submitError.set(extractErrorMessage(response));
          this.submitting.set(false);
        },
      });
    } else {
      const dto: CreateReservationRequest = {
        hotelId: this.hotelId,
        roomTypeId: value.roomTypeId,
        guestFullName: value.guestFullName,
        guestEmail: value.guestEmail,
        guestPhone: value.guestPhone,
        checkInDate: value.checkInDate,
        checkOutDate: value.checkOutDate,
        adultCount: value.adultCount,
        childCount: value.childCount,
        specialRequests: value.specialRequests || null,
      };

      this.reservationsService.create(dto).subscribe({
        next: (created) => this.router.navigate(['/reservations/success', created.referenceNumber]),
        error: (response: HttpErrorResponse) => {
          this.submitError.set(extractErrorMessage(response));
          this.submitting.set(false);
        },
      });
    }
  }

  private loadForCreate(): void {
    const hotelId = this.route.snapshot.queryParamMap.get('hotelId');
    const roomTypeId = this.route.snapshot.queryParamMap.get('roomTypeId');
    if (!hotelId) {
      this.loadError.set(true);
      this.loading.set(false);
      return;
    }

    this.hotelId = hotelId;

    this.hotelsService.getHotel(hotelId).subscribe({
      next: (hotel) => {
        this.hotelName.set(hotel.name);
        this.roomTypes.set(hotel.roomTypes);
        this.form.patchValue({ roomTypeId: roomTypeId ?? hotel.roomTypes[0]?.id ?? '' });
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  private loadForEdit(id: string): void {
    this.reservationsService.getMineById(id).subscribe({
      next: (reservation) => {
        this.hotelId = reservation.hotelId;
        this.hotelName.set(reservation.hotelName);
        this.form.patchValue({
          guestFullName: reservation.guestFullName,
          guestEmail: reservation.guestEmail,
          guestPhone: reservation.guestPhone,
          roomTypeId: reservation.roomTypeId,
          checkInDate: reservation.checkInDate,
          checkOutDate: reservation.checkOutDate,
          adultCount: reservation.adultCount,
          childCount: reservation.childCount,
          specialRequests: reservation.specialRequests ?? '',
        });
        // Guest identity isn't editable — see ReservationRequestService.UpdateMineAsync.
        this.form.controls.guestFullName.disable();
        this.form.controls.guestEmail.disable();
        this.form.controls.guestPhone.disable();

        this.hotelsService.getHotel(reservation.hotelId).subscribe({
          next: (hotel) => {
            this.roomTypes.set(hotel.roomTypes);
            this.loading.set(false);
          },
          error: () => {
            this.loadError.set(true);
            this.loading.set(false);
          },
        });
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }
}

function extractErrorMessage(response: HttpErrorResponse): string {
  const errors = response.error?.errors;
  if (errors && typeof errors === 'object') {
    const firstMessage = Object.values(errors).flat()[0];
    if (typeof firstMessage === 'string') {
      return firstMessage;
    }
  }

  return 'Could not save this reservation. Please check your details and try again.';
}
