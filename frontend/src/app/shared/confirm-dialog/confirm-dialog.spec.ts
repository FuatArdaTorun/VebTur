import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConfirmDialog } from './confirm-dialog';

describe('ConfirmDialog', () => {
  let fixture: ComponentFixture<ConfirmDialog>;
  let component: ConfirmDialog;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ConfirmDialog] });
    fixture = TestBed.createComponent(ConfirmDialog);
    component = fixture.componentInstance;
  });

  it('enables the confirm button immediately when no typed confirmation is required', () => {
    fixture.detectChanges();

    const confirmButton: HTMLButtonElement = fixture.nativeElement.querySelector('.confirm-dialog__confirm');
    expect(confirmButton.disabled).toBe(false);
  });

  it('disables the confirm button until the required text is typed exactly', () => {
    fixture.componentRef.setInput('requireTypedConfirmation', 'Grand Hotel');
    fixture.detectChanges();

    const confirmButton: HTMLButtonElement = fixture.nativeElement.querySelector('.confirm-dialog__confirm');
    const input: HTMLInputElement = fixture.nativeElement.querySelector('.confirm-dialog__typed-label input');
    expect(confirmButton.disabled).toBe(true);

    input.value = 'Grand Hote';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(confirmButton.disabled).toBe(true);

    input.value = 'Grand Hotel';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(confirmButton.disabled).toBe(false);
  });

  it('emits confirmed when the confirm button is clicked', () => {
    fixture.detectChanges();
    const confirmed = vi.fn();
    component.confirmed.subscribe(confirmed);

    fixture.nativeElement.querySelector('.confirm-dialog__confirm').click();

    expect(confirmed).toHaveBeenCalled();
  });

  it('emits cancelled when the cancel button is clicked', () => {
    fixture.detectChanges();
    const cancelled = vi.fn();
    component.cancelled.subscribe(cancelled);

    fixture.nativeElement.querySelector('.confirm-dialog__actions button').click();

    expect(cancelled).toHaveBeenCalled();
  });
});
