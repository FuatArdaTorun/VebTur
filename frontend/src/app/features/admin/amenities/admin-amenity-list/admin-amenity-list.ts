import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminAmenitiesService } from '../admin-amenities.service';
import { AdminAmenity } from '../models/admin-amenity.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { ConfirmDialog } from '../../../../shared/confirm-dialog/confirm-dialog';

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
  protected readonly pendingDelete = signal<AdminAmenity | null>(null);

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

  protected requestDelete(amenity: AdminAmenity): void {
    this.pendingDelete.set(amenity);
  }

  protected confirmDelete(): void {
    const amenity = this.pendingDelete();
    if (!amenity) {
      return;
    }

    this.service.deleteAmenity(amenity.id).subscribe(() => {
      this.pendingDelete.set(null);
      this.fetch();
    });
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(false);

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
