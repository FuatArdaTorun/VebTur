import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AdminHotelsService } from '../admin-hotels.service';
import { AdminHotelDetail, AdminHotelUpsert } from '../models/admin-hotel.model';
import { HotelsService } from '../../../hotels/hotels.service';
import { Amenity } from '../../../hotels/models/hotel.model';
import { UrlListEditor } from '../../../../shared/url-list-editor/url-list-editor';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { WarningBanner } from '../../../../shared/warning-banner/warning-banner';

interface ValidationProblemDetails {
  errors?: Record<string, string[]>;
}

@Component({
  selector: 'app-admin-hotel-form',
  imports: [ReactiveFormsModule, RouterLink, UrlListEditor, LoadingState, WarningBanner],
  templateUrl: './admin-hotel-form.html',
  styleUrl: './admin-hotel-form.scss',
})
export class AdminHotelForm {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly hotelsService = inject(AdminHotelsService);
  private readonly publicHotelsService = inject(HotelsService);

  private readonly hotelId = this.route.snapshot.paramMap.get('id');

  protected readonly isEditMode = this.hotelId !== null;
  protected readonly loading = signal(this.isEditMode);
  protected readonly saving = signal(false);
  protected readonly generalError = signal<string | null>(null);
  protected readonly amenitiesList = signal<Amenity[]>([]);

  protected readonly form: FormGroup = this.fb.group({
    name: ['', Validators.required],
    slug: ['', Validators.required],
    description: ['', Validators.required],
    city: ['', Validators.required],
    country: ['Turkey', Validators.required],
    address: ['', Validators.required],
    latitude: [0, Validators.required],
    longitude: [0, Validators.required],
    starRating: this.fb.control<number | null>(null),
    officialWebsiteUrl: this.fb.control<string | null>(null),
    googlePlaceId: this.fb.control<string | null>(null),
    phoneNumber: this.fb.control<string | null>(null),
    googleRating: this.fb.control<number | null>(null),
    googleRatingCount: this.fb.control<number | null>(null),
    isActive: [true],
    images: this.fb.array<FormGroup>([]),
    roomTypes: this.fb.array<FormGroup>([]),
    supervisors: this.fb.array<FormGroup>([]),
    amenitySlugs: this.fb.control<string[]>([]),
  });

  constructor() {
    this.publicHotelsService.getAmenities().subscribe((list) => this.amenitiesList.set(list));

    if (this.hotelId) {
      this.hotelsService.getHotel(this.hotelId).subscribe((hotel) => {
        this.patchForm(hotel);
        this.loading.set(false);
      });
    }
  }

  protected get imagesArray(): FormArray {
    return this.form.get('images') as FormArray;
  }

  protected get roomTypesArray(): FormArray {
    return this.form.get('roomTypes') as FormArray;
  }

  protected get supervisorsArray(): FormArray {
    return this.form.get('supervisors') as FormArray;
  }

  protected addRoomType(): void {
    this.roomTypesArray.push(
      this.fb.group({
        id: this.fb.control<string | null>(null),
        name: ['', Validators.required],
        description: ['', Validators.required],
        capacity: [2, Validators.required],
        baseNightlyPrice: [0, Validators.required],
        currency: ['TRY', Validators.required],
        availableCount: [0, Validators.required],
        isActive: [true],
      }),
    );
  }

  protected removeRoomType(index: number): void {
    this.roomTypesArray.removeAt(index);
  }

  protected addSupervisor(): void {
    this.supervisorsArray.push(
      this.fb.group({
        id: this.fb.control<string | null>(null),
        fullName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        isActive: [true],
      }),
    );
  }

  protected removeSupervisor(index: number): void {
    this.supervisorsArray.removeAt(index);
  }

  protected isAmenitySelected(slug: string): boolean {
    return (this.form.get('amenitySlugs')?.value as string[]).includes(slug);
  }

  protected toggleAmenity(slug: string): void {
    const control = this.form.get('amenitySlugs')!;
    const current = control.value as string[];
    control.setValue(current.includes(slug) ? current.filter((s) => s !== slug) : [...current, slug]);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.generalError.set('Please fix the highlighted fields.');
      return;
    }

    this.saving.set(true);
    this.generalError.set(null);
    this.clearServerErrors();

    const dto = this.form.getRawValue() as AdminHotelUpsert;
    const request = this.isEditMode
      ? this.hotelsService.updateHotel(this.hotelId!, dto)
      : this.hotelsService.createHotel(dto);

    request.subscribe({
      next: () => this.router.navigate(['/admin/hotels']),
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.applyServerErrors(err);
      },
    });
  }

  private patchForm(hotel: AdminHotelDetail): void {
    this.form.patchValue({
      name: hotel.name,
      slug: hotel.slug,
      description: hotel.description,
      city: hotel.city,
      country: hotel.country,
      address: hotel.address,
      latitude: hotel.latitude,
      longitude: hotel.longitude,
      starRating: hotel.starRating,
      officialWebsiteUrl: hotel.officialWebsiteUrl,
      googlePlaceId: hotel.googlePlaceId,
      phoneNumber: hotel.phoneNumber,
      googleRating: hotel.googleRating,
      googleRatingCount: hotel.googleRatingCount,
      isActive: hotel.isActive,
      amenitySlugs: hotel.amenitySlugs,
    });

    for (const image of hotel.images) {
      this.imagesArray.push(
        this.fb.group({
          id: this.fb.control<string | null>(image.id),
          url: [image.url, Validators.required],
          altText: this.fb.control<string | null>(image.altText),
          displayOrder: [image.displayOrder],
        }),
      );
    }

    for (const room of hotel.roomTypes) {
      this.roomTypesArray.push(
        this.fb.group({
          id: this.fb.control<string | null>(room.id),
          name: [room.name, Validators.required],
          description: [room.description, Validators.required],
          capacity: [room.capacity, Validators.required],
          baseNightlyPrice: [room.baseNightlyPrice, Validators.required],
          currency: [room.currency, Validators.required],
          availableCount: [room.availableCount, Validators.required],
          isActive: [room.isActive],
        }),
      );
    }

    for (const supervisor of hotel.supervisors) {
      this.supervisorsArray.push(
        this.fb.group({
          id: this.fb.control<string | null>(supervisor.id),
          fullName: [supervisor.fullName, Validators.required],
          email: [supervisor.email, [Validators.required, Validators.email]],
          isActive: [supervisor.isActive],
        }),
      );
    }
  }

  /** Maps a 400 ValidationProblemDetails body back onto the matching form controls. */
  private applyServerErrors(err: HttpErrorResponse): void {
    if (err.status !== 400) {
      this.generalError.set('Something went wrong while saving. Please try again.');
      return;
    }

    const body = err.error as ValidationProblemDetails;
    const errors = body?.errors;
    if (!errors) {
      this.generalError.set('Something went wrong while saving. Please try again.');
      return;
    }

    for (const [field, messages] of Object.entries(errors)) {
      const controlName = field.split('.')[0];
      const control = this.form.get(controlName.charAt(0).toLowerCase() + controlName.slice(1));
      if (control) {
        control.setErrors({ server: messages.join(' ') });
      } else {
        this.generalError.set(messages.join(' '));
      }
    }
  }

  private clearServerErrors(): void {
    for (const key of Object.keys(this.form.controls)) {
      const control = this.form.get(key);
      if (control?.errors?.['server']) {
        const { server, ...rest } = control.errors;
        control.setErrors(Object.keys(rest).length ? rest : null);
      }
    }
  }
}
