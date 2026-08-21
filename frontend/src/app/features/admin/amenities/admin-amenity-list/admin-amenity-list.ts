import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminAmenitiesService } from '../admin-amenities.service';
import { AdminAmenity } from '../models/admin-amenity.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../../shared/confirm-dialog/confirm-dialog';

type SortableColumn = 'name' | 'slug' | 'hotelCount';
type AmenitySort = 'name-asc' | 'name-desc' | 'slug-asc' | 'slug-desc' | 'hotelCount-asc' | 'hotelCount-desc';

@Component({
  selector: 'app-admin-amenity-list',
  imports: [ReactiveFormsModule, LoadingState, ErrorState, ConfirmDialog],
  templateUrl: './admin-amenity-list.html',
  styleUrl: './admin-amenity-list.scss',
})
export class AdminAmenityList {
  private readonly service = inject(AdminAmenitiesService);

  protected readonly amenities = signal<AdminAmenity[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly sort = signal<AmenitySort>('name-asc');

  protected readonly selectionMode = signal(false);
  protected readonly selectedIds = signal<Set<string>>(new Set());
  protected readonly confirmingBulkDelete = signal(false);

  protected readonly selectedCount = computed(() => this.selectedIds().size);
  protected readonly isAllSelected = computed(() => {
    const items = this.amenities();
    return items.length > 0 && items.every((a) => this.selectedIds().has(a.id));
  });

  /** Amenities are fetched unpaged in one go, so sorting is done client-side on the already-loaded list. */
  protected readonly sortedAmenities = computed(() => {
    const [column, direction] = this.sort().split('-') as [SortableColumn, 'asc' | 'desc'];
    const factor = direction === 'asc' ? 1 : -1;

    return [...this.amenities()].sort((a, b) =>
      column === 'hotelCount' ? (a.hotelCount - b.hotelCount) * factor : a[column].localeCompare(b[column]) * factor,
    );
  });

  protected readonly addForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    slug: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    iconKey: new FormControl<string | null>(null),
  });

  protected readonly editForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    slug: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    iconKey: new FormControl<string | null>(null),
  });

  constructor() {
    this.fetch();
  }

  protected addAmenity(): void {
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    this.service.createAmenity(this.addForm.getRawValue()).subscribe(() => {
      this.addForm.reset({ name: '', slug: '', iconKey: null });
      this.fetch();
    });
  }

  protected startEdit(amenity: AdminAmenity): void {
    this.editingId.set(amenity.id);
    this.editForm.setValue({ name: amenity.name, slug: amenity.slug, iconKey: amenity.iconKey });
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
  }

  protected saveEdit(id: string): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.service.updateAmenity(id, this.editForm.getRawValue()).subscribe(() => {
      this.editingId.set(null);
      this.fetch();
    });
  }

  /** Clicking a header toggles asc/desc if it's already the active column, otherwise starts ascending. */
  protected toggleSort(column: SortableColumn): void {
    const [activeColumn, activeDirection] = this.sort().split('-') as [string, string];
    const nextDirection = activeColumn === column && activeDirection === 'asc' ? 'desc' : 'asc';
    this.sort.set(`${column}-${nextDirection}` as AmenitySort);
  }

  protected sortIndicator(column: SortableColumn): string {
    const [activeColumn, activeDirection] = this.sort().split('-') as [string, string];
    if (activeColumn !== column) {
      return '';
    }

    return activeDirection === 'asc' ? '▲' : '▼';
  }

  /** Toggling off drops any in-progress selection so re-entering selection mode starts fresh. */
  protected toggleSelectionMode(): void {
    this.selectionMode.set(!this.selectionMode());
    this.selectedIds.set(new Set());
  }

  protected isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  protected toggleSelect(id: string): void {
    const next = new Set(this.selectedIds());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.selectedIds.set(next);
  }

  protected toggleSelectAll(): void {
    this.selectedIds.set(this.isAllSelected() ? new Set() : new Set(this.amenities().map((a) => a.id)));
  }

  protected requestBulkDelete(): void {
    if (this.selectedCount() > 0) {
      this.confirmingBulkDelete.set(true);
    }
  }

  protected confirmBulkDelete(): void {
    this.service.deleteAmenities([...this.selectedIds()]).subscribe(() => {
      this.confirmingBulkDelete.set(false);
      this.selectionMode.set(false);
      this.selectedIds.set(new Set());
      this.fetch();
    });
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(false);
    this.selectedIds.set(new Set());

    this.service.getAmenities().subscribe({
      next: (list) => {
        this.amenities.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
