import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';
import { extractErrorMessage } from '../../../core/http/extract-error-message';

@Component({
  selector: 'app-profile-security',
  imports: [ReactiveFormsModule],
  templateUrl: './profile-security.html',
  styleUrl: './profile-security.scss',
})
export class ProfileSecurity {
  private readonly authService = inject(AuthService);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly saved = signal(false);

  protected readonly form = new FormGroup(
    {
      currentPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      newPassword: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] }),
      confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    },
    { validators: [passwordsMatchValidator] },
  );

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.saved.set(false);
    const value = this.form.getRawValue();

    this.authService.changePassword({ currentPassword: value.currentPassword, newPassword: value.newPassword }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.saved.set(true);
        this.form.reset({ currentPassword: '', newPassword: '', confirmPassword: '' });
      },
      error: (response: HttpErrorResponse) => {
        this.submitting.set(false);
        this.error.set(extractErrorMessage(response, 'Could not update your password. Please check your current password and try again.'));
      },
    });
  }
}

function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const newPassword = control.get('newPassword')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return newPassword && confirmPassword && newPassword !== confirmPassword ? { passwordMismatch: true } : null;
}
