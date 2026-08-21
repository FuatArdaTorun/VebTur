import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AdminAmenityList } from './admin-amenity-list';
import { AdminAmenitiesService } from '../admin-amenities.service';
import { AdminAmenity } from '../models/admin-amenity.model';

describe('AdminAmenityList', () => {
  let fixture: ComponentFixture<AdminAmenityList>;
  let component: AdminAmenityList;
  let serviceStub: {
    getAmenities: ReturnType<typeof vi.fn>;
    createAmenity?: ReturnType<typeof vi.fn>;
    updateAmenity?: ReturnType<typeof vi.fn>;
    deleteAmenities?: ReturnType<typeof vi.fn>;
  };

  const sampleAmenity: AdminAmenity = { id: 'a-1', name: 'Sea View', slug: 'sea-view', iconKey: null, hotelCount: 3 };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [AdminAmenityList],
      providers: [{ provide: AdminAmenitiesService, useValue: serviceStub }],
    });

    fixture = TestBed.createComponent(AdminAmenityList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads and displays amenities on init', () => {
    serviceStub = { getAmenities: vi.fn().mockReturnValue(of([sampleAmenity])) };
    createComponent();

    expect(component['amenities']()).toEqual([sampleAmenity]);
    expect(component['loading']()).toBe(false);
  });

  it('shows the error state when the fetch fails', () => {
    serviceStub = { getAmenities: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) };
    createComponent();

    expect(component['error']()).toBe(true);
    expect(component['loading']()).toBe(false);
  });

  it('selection mode is off by default, with no checkboxes rendered', () => {
    serviceStub = { getAmenities: vi.fn().mockReturnValue(of([sampleAmenity])) };
    createComponent();

    expect(component['selectionMode']()).toBe(false);
    expect(fixture.nativeElement.querySelector('input[type="checkbox"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Delete Amenity');
  });

  it('toggling selection mode on shows a checkbox per amenity; toggling it off clears any selection', () => {
    serviceStub = { getAmenities: vi.fn().mockReturnValue(of([sampleAmenity])) };
    createComponent();

    component['toggleSelectionMode']();
    fixture.detectChanges();
    expect(component['selectionMode']()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('input[type="checkbox"]').length).toBe(2); // header select-all + one row

    component['toggleSelect'](sampleAmenity.id);
    expect(component['selectedCount']()).toBe(1);

    component['toggleSelectionMode']();
    expect(component['selectionMode']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
  });

  it('select-all selects every currently loaded amenity, and toggles them all off again', () => {
    const second: AdminAmenity = { ...sampleAmenity, id: 'a-2', name: 'Pool' };
    serviceStub = { getAmenities: vi.fn().mockReturnValue(of([sampleAmenity, second])) };
    createComponent();
    component['toggleSelectionMode']();

    component['toggleSelectAll']();
    expect(component['isAllSelected']()).toBe(true);
    expect(component['selectedCount']()).toBe(2);

    component['toggleSelectAll']();
    expect(component['isAllSelected']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
  });

  it('requestBulkDelete opens the confirm dialog only when something is selected', () => {
    serviceStub = { getAmenities: vi.fn().mockReturnValue(of([sampleAmenity])) };
    createComponent();
    component['toggleSelectionMode']();

    component['requestBulkDelete']();
    expect(component['confirmingBulkDelete']()).toBe(false);

    component['toggleSelect'](sampleAmenity.id);
    component['requestBulkDelete']();
    expect(component['confirmingBulkDelete']()).toBe(true);
  });

  it('confirming bulk delete calls the service with the selected ids, exits selection mode, and refetches', () => {
    serviceStub = {
      getAmenities: vi.fn().mockReturnValue(of([sampleAmenity])),
      deleteAmenities: vi.fn().mockReturnValue(of(undefined)),
    };
    createComponent();
    component['toggleSelectionMode']();
    component['toggleSelect'](sampleAmenity.id);
    component['requestBulkDelete']();

    component['confirmBulkDelete']();

    expect(serviceStub.deleteAmenities).toHaveBeenCalledWith(['a-1']);
    expect(component['confirmingBulkDelete']()).toBe(false);
    expect(component['selectionMode']()).toBe(false);
    expect(component['selectedCount']()).toBe(0);
    expect(serviceStub.getAmenities).toHaveBeenCalledTimes(2);
  });

  it('startEdit populates the edit form and editingId; cancelEdit clears it', () => {
    serviceStub = { getAmenities: vi.fn().mockReturnValue(of([sampleAmenity])) };
    createComponent();

    component['startEdit'](sampleAmenity);
    expect(component['editingId']()).toBe('a-1');
    expect(component['editForm'].value).toEqual({ name: 'Sea View', slug: 'sea-view', iconKey: null });

    component['cancelEdit']();
    expect(component['editingId']()).toBeNull();
  });

  it('sortedAmenities defaults to name ascending', () => {
    const zAmenity: AdminAmenity = { id: 'a-2', name: 'Zen Garden', slug: 'zen-garden', iconKey: null, hotelCount: 1 };
    serviceStub = { getAmenities: vi.fn().mockReturnValue(of([zAmenity, sampleAmenity])) };
    createComponent();

    expect(component['sortedAmenities']().map((a) => a.name)).toEqual(['Sea View', 'Zen Garden']);
  });

  it('clicking a column header sorts ascending by that column, without refetching (client-side only)', () => {
    const zAmenity: AdminAmenity = { id: 'a-2', name: 'Zen Garden', slug: 'zen-garden', iconKey: null, hotelCount: 1 };
    serviceStub = { getAmenities: vi.fn().mockReturnValue(of([sampleAmenity, zAmenity])) };
    createComponent();

    component['toggleSort']('hotelCount');

    expect(component['sort']()).toBe('hotelCount-asc');
    expect(component['sortIndicator']('hotelCount')).toBe('▲');
    expect(component['sortedAmenities']().map((a) => a.hotelCount)).toEqual([1, 3]);
    expect(serviceStub.getAmenities).toHaveBeenCalledTimes(1);
  });

  it('clicking the same column header again toggles to descending', () => {
    const zAmenity: AdminAmenity = { id: 'a-2', name: 'Zen Garden', slug: 'zen-garden', iconKey: null, hotelCount: 1 };
    serviceStub = { getAmenities: vi.fn().mockReturnValue(of([sampleAmenity, zAmenity])) };
    createComponent();

    // Default sort is already 'name-asc', so a single click on the active column flips it.
    component['toggleSort']('name');

    expect(component['sort']()).toBe('name-desc');
    expect(component['sortIndicator']('name')).toBe('▼');
    expect(component['sortedAmenities']().map((a) => a.name)).toEqual(['Zen Garden', 'Sea View']);
  });
});
