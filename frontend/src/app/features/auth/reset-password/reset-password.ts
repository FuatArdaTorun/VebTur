import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';
import { AuthLayout } from '../../../shared/auth-layout/auth-layout';
import { extractErrorMessage } from '../../../core/http/extract-error-message';

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink, AuthLayout],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.scss',
})
export class ResetPassword {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  private readonly email = this.route.snapshot.queryParamMap.get('email');
  private readonly token = this.route.snapshot.queryParamMap.get('token');

  protected readonly linkIsValid = !!this.email && !!this.token;

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly done = signal(false);

  protected readonly form = new FormGroup(
    {
      newPassword: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] }),
      confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    },
    { validators: [passwordsMatchValidator] },
  );

  protected submit(): void {
    if (this.form.invalid || !this.email || !this.token) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.authService.resetPassword({ email: this.email, token: this.token, newPassword: this.form.getRawValue().newPassword }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.done.set(true);
      },
      error: (response: HttpErrorResponse) => {
        this.submitting.set(false);
        this.error.set(extractErrorMessage(response, 'This password reset link is invalid or has expired. Please request a new one.'));
      },
    });
  }
}

function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const newPassword = control.get('newPassword')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return newPassword && confirmPassword && newPassword !== confirmPassword ? { passwordMismatch: true } : null;
}
