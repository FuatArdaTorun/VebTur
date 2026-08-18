import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';
import { Gender } from '../../../core/auth/auth.models';
import { extractErrorMessage } from '../../../core/http/extract-error-message';
import { LoadingState } from '../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../shared/error-state/error-state';

@Component({
  selector: 'app-profile-info',
  imports: [ReactiveFormsModule, LoadingState, ErrorState],
  templateUrl: './profile-info.html',
  styleUrl: './profile-info.scss',
})
export class ProfileInfo {
  private readonly authService = inject(AuthService);

  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly saved = signal(false);
  protected readonly email = signal('');

  protected readonly genders: Gender[] = ['Female', 'Male', 'Other', 'PreferNotToSay'];
  protected readonly genderLabels: Record<Gender, string> = {
    Female: 'Female',
    Male: 'Male',
    Other: 'Other',
    PreferNotToSay: 'Prefer not to say',
  };

  protected readonly form = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(3)] }),
    phoneNumber: new FormControl('', { nonNullable: true }),
    firstName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true }),
    gender: new FormControl<Gender | ''>('', { nonNullable: true }),
    dateOfBirth: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    this.authService.getProfile().subscribe({
      next: (profile) => {
        this.email.set(profile.email);
        this.form.patchValue({
          userName: profile.userName,
          phoneNumber: profile.phoneNumber ?? '',
          firstName: profile.firstName ?? '',
          lastName: profile.lastName ?? '',
          gender: profile.gender ?? '',
          dateOfBirth: profile.dateOfBirth ?? '',
        });
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);
    this.saved.set(false);
    const value = this.form.getRawValue();

    this.authService
      .updateProfile({
        userName: value.userName,
        phoneNumber: value.phoneNumber || null,
        firstName: value.firstName || null,
        lastName: value.lastName || null,
        gender: value.gender || null,
        dateOfBirth: value.dateOfBirth || null,
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.saved.set(true);
        },
        error: (response: HttpErrorResponse) => {
          this.submitting.set(false);
          this.submitError.set(extractErrorMessage(response, 'Could not save your profile. Please try again.'));
        },
      });
  }
}
