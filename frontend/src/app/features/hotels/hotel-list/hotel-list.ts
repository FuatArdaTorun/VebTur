import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { HotelsService } from '../hotels.service';
import { Amenity, HotelSearchParams, HotelSortOrder, HotelSummary } from '../models/hotel.model';
import { HotelCard } from '../../../shared/hotel-card/hotel-card';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';

const PAGE_SIZE = 12;
const CITIES = ['Antalya', 'Istanbul', 'Izmir', 'Bodrum', 'Fethiye', 'Cappadocia'];

interface FilterFormValue {
  city: string | null;
  minPrice: number | null;
  maxPrice: number | null;
  minStarRating: number | null;
  minCapacity: number | null;
  sort: HotelSortOrder;
}

@Component({
  selector: 'app-hotel-list',
  imports: [HotelCard, LoadingState, EmptyState, ErrorState, ReactiveFormsModule],
  templateUrl: './hotel-list.html',
  styleUrl: './hotel-list.scss',
})
export class HotelList {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly hotelsService = inject(HotelsService);

  protected readonly cities = CITIES;

  protected readonly hotels = signal<HotelSummary[]>([]);
  protected readonly amenitiesList = signal<Amenity[]>([]);
  protected readonly selectedAmenities = signal<string[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly totalCount = signal(0);

  protected readonly filterForm = new FormGroup({
    city: new FormControl<string | null>(null),
    minPrice: new FormControl<number | null>(null),
    maxPrice: new FormControl<number | null>(null),
    minStarRating: new FormControl<number | null>(null),
    minCapacity: new FormControl<number | null>(null),
    sort: new FormControl<HotelSortOrder>('recommended', { nonNullable: true }),
  });

  constructor() {
    this.hotelsService.getAmenities().subscribe((list) => this.amenitiesList.set(list));

    this.route.queryParamMap.subscribe((params) => {
      const toNumber = (key: string) => (params.get(key) ? Number(params.get(key)) : null);

      this.filterForm.patchValue(
        {
          city: params.get('city'),
          minPrice: toNumber('minPrice'),
          maxPrice: toNumber('maxPrice'),
          minStarRating: toNumber('minStarRating'),
          minCapacity: toNumber('minCapacity'),
          sort: (params.get('sort') as HotelSortOrder) ?? 'recommended',
        },
        { emitEvent: false },
      );
      this.selectedAmenities.set(params.getAll('amenities'));

      const page = toNumber('page') ?? 1;
      this.page.set(page);
      this.fetchHotels(page);
    });
  }

  protected toggleAmenity(slug: string): void {
    const current = this.selectedAmenities();
    this.selectedAmenities.set(
      current.includes(slug) ? current.filter((s) => s !== slug) : [...current, slug],
    );
  }

  protected applyFilters(): void {
    this.navigate(1);
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.navigate(page);
  }

  private navigate(page: number): void {
    const v = this.filterForm.value as FilterFormValue;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        city: v.city || null,
        minPrice: v.minPrice || null,
        maxPrice: v.maxPrice || null,
        minStarRating: v.minStarRating || null,
        minCapacity: v.minCapacity || null,
        amenities: this.selectedAmenities().length ? this.selectedAmenities() : null,
        sort: v.sort && v.sort !== 'recommended' ? v.sort : null,
        page: page > 1 ? page : null,
      },
    });
  }

  private fetchHotels(page: number): void {
    this.loading.set(true);
    this.error.set(false);

    const v = this.filterForm.value as FilterFormValue;
    const params: HotelSearchParams = {
      city: v.city ?? undefined,
      minPrice: v.minPrice ?? undefined,
      maxPrice: v.maxPrice ?? undefined,
      minStarRating: v.minStarRating ?? undefined,
      minCapacity: v.minCapacity ?? undefined,
      amenities: this.selectedAmenities().length ? this.selectedAmenities() : undefined,
      sort: v.sort,
      page,
      pageSize: PAGE_SIZE,
    };

    this.hotelsService.getHotels(params).subscribe({
      next: (result) => {
        this.hotels.set(result.items);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
