import { Component, computed, input, output, signal } from '@angular/core';

/** Rendered by the parent inside an `@if`, so mounting IS the "open" state. */
@Component({
  selector: 'app-confirm-dialog',
  imports: [],
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss',
})
export class ConfirmDialog {
  readonly message = input('Are you sure?');
  readonly confirmLabel = input('Confirm');
  readonly cancelLabel = input('Cancel');

  /**
   * When set, the confirm button stays disabled until the user types this exact text — an extra
   * safety gate for irreversible actions (e.g. permanently deleting a hotel), matching this
   * project's rule that destructive actions get a strong confirmation, not just a click-through.
   */
  readonly requireTypedConfirmation = input<string | null>(null);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  protected readonly typedValue = signal('');
  protected readonly canConfirm = computed(() => {
    const required = this.requireTypedConfirmation();
    return required === null || this.typedValue() === required;
  });

  protected onTypedInput(value: string): void {
    this.typedValue.set(value);
  }
}
