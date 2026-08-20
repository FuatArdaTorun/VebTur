import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AdminSupportMessagesService } from '../admin-support-messages.service';
import { AdminSupportMessage } from '../models/admin-support-message.model';
import { LoadingState } from '../../../../shared/loading-state/loading-state';
import { ErrorState } from '../../../../shared/error-state/error-state';
import { extractErrorMessage } from '../../../../core/http/extract-error-message';

@Component({
  selector: 'app-admin-support-message-detail',
  imports: [RouterLink, DatePipe, ReactiveFormsModule, LoadingState, ErrorState],
  templateUrl: './admin-support-message-detail.html',
  styleUrl: './admin-support-message-detail.scss',
})
export class AdminSupportMessageDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly messagesService = inject(AdminSupportMessagesService);

  protected readonly message = signal<AdminSupportMessage | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  protected readonly replyForm = new FormGroup({
    replyMessage: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    this.fetch();
  }

  protected submitReply(): void {
    if (this.replyForm.invalid) {
      this.replyForm.markAllAsTouched();
      return;
    }

    const id = this.message()?.id;
    if (!id) {
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);

    this.messagesService.reply(id, this.replyForm.getRawValue().replyMessage).subscribe({
      next: () => {
        this.submitting.set(false);
        this.fetch();
      },
      error: (response: HttpErrorResponse) => {
        this.submitting.set(false);
        this.submitError.set(extractErrorMessage(response, 'Could not save your reply. Please try again.'));
      },
    });
  }

  private fetch(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loadError.set(true);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.loadError.set(false);

    this.messagesService.getMessage(id).subscribe({
      next: (result) => {
        this.message.set(result);
        this.replyForm.patchValue({ replyMessage: result.replyMessage ?? '' });
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }
}
