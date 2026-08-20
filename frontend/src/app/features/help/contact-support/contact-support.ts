import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { HelpService } from '../help.service';
import { AuthService } from '../../../core/auth/auth.service';
import { extractErrorMessage } from '../../../core/http/extract-error-message';

@Component({
  selector: 'app-contact-support',
  imports: [ReactiveFormsModule],
  templateUrl: './contact-support.html',
  styleUrl: './contact-support.scss',
})
export class ContactSupport {
  private readonly helpService = inject(HelpService);
  protected readonly authService = inject(AuthService);

  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly submitted = signal(false);
  protected readonly useMyInfo = signal(false);

  protected readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    subject: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    message: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected onUseMyInfoChange(checked: boolean): void {
    this.useMyInfo.set(checked);

    if (!checked) {
      this.form.patchValue({ name: '', email: '' });
      return;
    }

    this.authService.getProfile().subscribe((profile) => {
      this.form.patchValue({ name: profile.displayName, email: profile.email });
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);

    this.helpService.sendMessage(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
      },
      error: (response: HttpErrorResponse) => {
        this.submitting.set(false);
        this.submitError.set(extractErrorMessage(response, 'Could not send your message. Please try again.'));
      },
    });
  }
}
