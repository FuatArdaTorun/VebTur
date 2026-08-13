import { Component, input } from '@angular/core';
import { AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

/**
 * Add/remove/reorder editor for a FormArray of `{ id, url, altText, displayOrder }` rows —
 * there's no file-upload infrastructure anywhere in this app, so admin
 * image management is a URL-list editor, matching how HotelImage.Url is
 * already just a string. Takes the FormArray directly rather than implementing
 * ControlValueAccessor — this is its only use site today.
 */
@Component({
  selector: 'app-url-list-editor',
  imports: [ReactiveFormsModule],
  templateUrl: './url-list-editor.html',
  styleUrl: './url-list-editor.scss',
})
export class UrlListEditor {
  readonly formArray = input.required<FormArray>();
  readonly label = input('Images');

  protected asGroup(control: AbstractControl): FormGroup {
    return control as FormGroup;
  }

  protected addRow(): void {
    const array = this.formArray();
    array.push(
      new FormGroup({
        id: new FormControl<string | null>(null),
        url: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
        altText: new FormControl<string | null>(null),
        displayOrder: new FormControl(array.length + 1, { nonNullable: true }),
      }),
    );
  }

  protected removeRow(index: number): void {
    this.formArray().removeAt(index);
    this.renumber();
  }

  protected moveUp(index: number): void {
    if (index === 0) {
      return;
    }

    this.swap(index, index - 1);
  }

  protected moveDown(index: number): void {
    if (index === this.formArray().length - 1) {
      return;
    }

    this.swap(index, index + 1);
  }

  protected onImageError(event: Event): void {
    (event.target as HTMLImageElement).classList.add('url-list-editor__preview--broken');
  }

  private swap(a: number, b: number): void {
    const array = this.formArray();
    const control = array.at(a);
    array.removeAt(a);
    array.insert(b, control);
    this.renumber();
  }

  private renumber(): void {
    this.formArray().controls.forEach((control, index) => control.get('displayOrder')?.setValue(index + 1));
  }
}
