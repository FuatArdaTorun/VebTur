import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';
import { AuthLayout } from '../../../shared/auth-layout/auth-layout';
import { extractErrorMessage } from '../../../core/http/extract-error-message';
import { ForgotPasswordResponse } from '../../../core/auth/auth.models';

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink, AuthLayout],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss',
})
export class ForgotPassword {
  private readonly authService = inject(AuthService);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly result = signal<ForgotPasswordResponse | null>(null);

  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.authService.forgotPassword(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.result.set(response);
      },
      error: (response: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(extractErrorMessage(response, 'Something went wrong. Please check the email address and try again.'));
      },
    });
  }
}
