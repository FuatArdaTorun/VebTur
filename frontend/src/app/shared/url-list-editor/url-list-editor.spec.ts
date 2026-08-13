import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormArray, FormGroup } from '@angular/forms';

import { UrlListEditor } from './url-list-editor';

/** Exposes the component's protected methods for direct invocation in tests. */
interface UrlListEditorInternals {
  addRow(): void;
  removeRow(index: number): void;
  moveUp(index: number): void;
  moveDown(index: number): void;
}

describe('UrlListEditor', () => {
  let fixture: ComponentFixture<UrlListEditor>;
  let component: UrlListEditor & UrlListEditorInternals;
  let formArray: FormArray;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [UrlListEditor] }).compileComponents();

    fixture = TestBed.createComponent(UrlListEditor);
    formArray = new FormArray<FormGroup>([]);
    fixture.componentRef.setInput('formArray', formArray);
    component = fixture.componentInstance as UrlListEditor & UrlListEditorInternals;
    fixture.detectChanges();
  });

  function urls(): string[] {
    return formArray.controls.map((c) => c.get('url')?.value);
  }

  function displayOrders(): number[] {
    return formArray.controls.map((c) => c.get('displayOrder')?.value);
  }

  it('addRow pushes a new row with the next sequential displayOrder', () => {
    component.addRow();
    component.addRow();

    expect(formArray.length).toBe(2);
    expect(displayOrders()).toEqual([1, 2]);
  });

  it('removeRow removes the target row and renumbers the rest', () => {
    component.addRow();
    component.addRow();
    component.addRow();
    formArray.at(0).get('url')?.setValue('first');
    formArray.at(1).get('url')?.setValue('second');
    formArray.at(2).get('url')?.setValue('third');

    component.removeRow(1);

    expect(urls()).toEqual(['first', 'third']);
    expect(displayOrders()).toEqual([1, 2]);
  });

  it('moveUp swaps a row with its predecessor and renumbers', () => {
    component.addRow();
    component.addRow();
    formArray.at(0).get('url')?.setValue('first');
    formArray.at(1).get('url')?.setValue('second');

    component.moveUp(1);

    expect(urls()).toEqual(['second', 'first']);
    expect(displayOrders()).toEqual([1, 2]);
  });

  it('moveUp on the first row is a no-op', () => {
    component.addRow();
    component.addRow();
    formArray.at(0).get('url')?.setValue('first');
    formArray.at(1).get('url')?.setValue('second');

    component.moveUp(0);

    expect(urls()).toEqual(['first', 'second']);
  });

  it('moveDown swaps a row with its successor and renumbers', () => {
    component.addRow();
    component.addRow();
    formArray.at(0).get('url')?.setValue('first');
    formArray.at(1).get('url')?.setValue('second');

    component.moveDown(0);

    expect(urls()).toEqual(['second', 'first']);
    expect(displayOrders()).toEqual([1, 2]);
  });

  it('moveDown on the last row is a no-op', () => {
    component.addRow();
    component.addRow();
    formArray.at(0).get('url')?.setValue('first');
    formArray.at(1).get('url')?.setValue('second');

    component.moveDown(1);

    expect(urls()).toEqual(['first', 'second']);
  });
});
