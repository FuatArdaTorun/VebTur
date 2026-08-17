import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { HotelsService } from '../hotels.service';
import {
  CreateReviewRequest,
  ExternalRating,
  HotelDetail as HotelDetailModel,
  Review,
  ReviewableReservation,
} from '../models/hotel.model';
import { AuthService } from '../../../core/auth/auth.service';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../shared/error-state/error-state';

@Component({
  selector: 'app-hotel-detail',
  imports: [LoadingState, ErrorState, RouterLink, DecimalPipe, DatePipe, ReactiveFormsModule],
  templateUrl: './hotel-detail.html',
  styleUrl: './hotel-detail.scss',
})
export class HotelDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly hotelsService = inject(HotelsService);
  protected readonly authService = inject(AuthService);

  protected readonly hotel = signal<HotelDetailModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  // Loaded separately, after the main hotel record, so a slow/unreachable external rating
  // provider never delays the rest of the page.
  protected readonly externalRating = signal<ExternalRating | null>(null);
  protected readonly externalRatingLoading = signal(true);

  // Also loaded separately, same reasoning — reviews shouldn't block the rest of the page either.
  protected readonly reviews = signal<Review[]>([]);
  protected readonly reviewableReservations = signal<ReviewableReservation[]>([]);
  protected readonly reviewsLoading = signal(true);

  protected readonly reviewSubmitting = signal(false);
  protected readonly reviewSubmitError = signal<string | null>(null);
  protected readonly reviewSubmitted = signal(false);

  protected readonly reviewForm = new FormGroup({
    reservationRequestId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    rating: new FormControl(0, { nonNullable: true, validators: [Validators.min(1)] }),
    comment: new FormControl('', { nonNullable: true }),
  });

  private hotelId = '';

  constructor() {
    const idOrSlug = this.route.snapshot.paramMap.get('idOrSlug');
    if (!idOrSlug) {
      this.error.set(true);
      this.loading.set(false);
      this.externalRatingLoading.set(false);
      this.reviewsLoading.set(false);
      return;
    }

    this.hotelsService.getHotel(idOrSlug).subscribe({
      next: (hotel) => {
        this.hotel.set(hotel);
        this.hotelId = hotel.id;
        this.loading.set(false);
        this.fetchExternalRating(hotel.id);
        this.fetchReviews(hotel.id);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
        this.externalRatingLoading.set(false);
        this.reviewsLoading.set(false);
      },
    });
  }

  protected mapUrl(hotel: HotelDetailModel): string {
    // Search by name + address (not just coordinates) so Google Maps resolves directly to
    // the hotel's own business listing — with its photos, reviews, and info card — rather
    // than dropping a bare pin at a nearby coordinate.
    const query = encodeURIComponent(`${hotel.name}, ${hotel.address}, ${hotel.city}, ${hotel.country}`);
    return `https://www.google.com/maps/search/?api=1&query=${query}`;
  }

  protected setRating(value: number): void {
    this.reviewForm.controls.rating.setValue(value);
  }

  protected starDisplay(rating: number): string {
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  }

  protected submitReview(): void {
    if (this.reviewForm.invalid) {
      this.reviewForm.markAllAsTouched();
      return;
    }

    this.reviewSubmitting.set(true);
    this.reviewSubmitError.set(null);
    const value = this.reviewForm.getRawValue();

    const dto: CreateReviewRequest = {
      reservationRequestId: value.reservationRequestId,
      rating: value.rating,
      comment: value.comment || null,
    };

    this.hotelsService.createReview(this.hotelId, dto).subscribe({
      next: (created) => {
        this.reviews.update((current) => [created, ...current]);
        this.reviewableReservations.update((current) =>
          current.filter((r) => r.reservationRequestId !== dto.reservationRequestId),
        );
        // The hotel record was fetched before this review existed — patch its aggregate in
        // place rather than re-fetching, so the summary above the list doesn't read stale.
        this.hotel.update((h) => {
          if (!h) return h;
          const newCount = h.customerReviewCount + 1;
          const priorTotal = (h.customerRating ?? 0) * h.customerReviewCount;
          return { ...h, customerReviewCount: newCount, customerRating: (priorTotal + created.rating) / newCount };
        });
        this.reviewSubmitting.set(false);
        this.reviewSubmitted.set(true);
        this.reviewForm.reset({ reservationRequestId: '', rating: 0, comment: '' });
      },
      error: (response: HttpErrorResponse) => {
        this.reviewSubmitError.set(extractErrorMessage(response));
        this.reviewSubmitting.set(false);
      },
    });
  }

  private fetchExternalRating(hotelId: string): void {
    this.hotelsService.getExternalRating(hotelId).subscribe({
      next: (rating) => {
        this.externalRating.set(rating);
        this.externalRatingLoading.set(false);
      },
      // A failure here is never fatal to the page — it just means no rating badge shows.
      error: () => this.externalRatingLoading.set(false),
    });
  }

  private fetchReviews(hotelId: string): void {
    this.hotelsService.getHotelReviews(hotelId).subscribe({
      next: (result) => {
        this.reviews.set(result.reviews);
        this.reviewableReservations.set(result.myReviewableReservations);
        if (result.myReviewableReservations.length > 0) {
          this.reviewForm.patchValue({ reservationRequestId: result.myReviewableReservations[0].reservationRequestId });
        }
        this.reviewsLoading.set(false);
      },
      // A failure here is never fatal to the page — it just means no reviews section shows.
      error: () => this.reviewsLoading.set(false),
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

  return 'Could not submit your review. Please try again.';
}
