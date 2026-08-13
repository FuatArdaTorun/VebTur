import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { FormArray, FormGroup } from '@angular/forms';
import { of } from 'rxjs';

import { AdminHotelForm } from './admin-hotel-form';
import { AdminHotelsService } from '../admin-hotels.service';
import { AdminHotelDetail } from '../models/admin-hotel.model';
import { HotelsService } from '../../../hotels/hotels.service';

interface AdminHotelFormInternals {
  isEditMode: boolean;
  form: FormGroup;
  imagesArray: FormArray;
  roomTypesArray: FormArray;
  supervisorsArray: FormArray;
  addRoomType(): void;
  addSupervisor(): void;
  toggleAmenity(slug: string): void;
  submit(): void;
}

const sampleDetail: AdminHotelDetail = {
  id: 'abc',
  name: 'Existing Hotel',
  slug: 'existing-hotel',
  description: 'A hotel that already exists.',
  city: 'Antalya',
  country: 'Turkey',
  address: 'Some Address 1',
  latitude: 36.9,
  longitude: 30.7,
  starRating: 5,
  officialWebsiteUrl: null,
  googlePlaceId: null,
  phoneNumber: null,
  googleRating: null,
  googleRatingCount: null,
  isActive: true,
  createdAtUtc: new Date().toISOString(),
  updatedAtUtc: new Date().toISOString(),
  images: [{ id: 'img-1', url: 'https://example.com/a.jpg', altText: null, displayOrder: 1 }],
  roomTypes: [{ id: 'room-1', name: 'Standard', description: 'd', capacity: 2, baseNightlyPrice: 1000, currency: 'TRY', isActive: true }],
  supervisors: [{ id: 'sup-1', fullName: 'Jane Doe', email: 'jane@example.com', isActive: true }],
  amenitySlugs: ['wifi'],
};

@Component({ template: '' })
class BlankStub {}

describe('AdminHotelForm', () => {
  let fixture: ComponentFixture<AdminHotelForm>;
  let component: AdminHotelForm & AdminHotelFormInternals;
  let adminHotelsServiceStub: { getHotel: ReturnType<typeof vi.fn>; createHotel: ReturnType<typeof vi.fn>; updateHotel: ReturnType<typeof vi.fn> };

  function createComponent(id: string | null): void {
    adminHotelsServiceStub = {
      getHotel: vi.fn().mockReturnValue(of(sampleDetail)),
      createHotel: vi.fn().mockReturnValue(of(sampleDetail)),
      updateHotel: vi.fn().mockReturnValue(of(sampleDetail)),
    };

    TestBed.configureTestingModule({
      imports: [AdminHotelForm],
      providers: [
        // A real match for '/admin/hotels' — submit() navigates there on success, and an
        // unmatched route would otherwise surface as an unhandled rejection in these tests.
        provideRouter([{ path: 'admin/hotels', component: BlankStub }]),
        { provide: AdminHotelsService, useValue: adminHotelsServiceStub },
        { provide: HotelsService, useValue: { getAmenities: vi.fn().mockReturnValue(of([])) } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } } },
      ],
    });

    fixture = TestBed.createComponent(AdminHotelForm);
    component = fixture.componentInstance as AdminHotelForm & AdminHotelFormInternals;
    fixture.detectChanges();
  }

  it('create mode starts with an empty form and no nested rows', () => {
    createComponent(null);

    expect(component.isEditMode).toBe(false);
    expect(adminHotelsServiceStub.getHotel).not.toHaveBeenCalled();
    expect(component.imagesArray.length).toBe(0);
    expect(component.roomTypesArray.length).toBe(0);
    expect(component.supervisorsArray.length).toBe(0);
  });

  it('addRoomType/addSupervisor push new rows onto their FormArrays', () => {
    createComponent(null);

    component.addRoomType();
    component.addSupervisor();

    expect(component.roomTypesArray.length).toBe(1);
    expect(component.supervisorsArray.length).toBe(1);
  });

  it('edit mode loads and patches the existing hotel, including nested arrays', () => {
    createComponent('abc');

    expect(adminHotelsServiceStub.getHotel).toHaveBeenCalledWith('abc');
    expect(component.form.get('name')?.value).toBe(sampleDetail.name);
    expect(component.form.get('amenitySlugs')?.value).toEqual(['wifi']);
    expect(component.imagesArray.length).toBe(1);
    expect(component.roomTypesArray.length).toBe(1);
    expect(component.supervisorsArray.length).toBe(1);
    expect(component.imagesArray.at(0).get('url')?.value).toBe('https://example.com/a.jpg');
  });

  it('submit does not call the service and marks controls touched when the form is invalid', () => {
    createComponent(null);

    component.submit();

    expect(adminHotelsServiceStub.createHotel).not.toHaveBeenCalled();
    expect(component.form.get('name')?.touched).toBe(true);
  });

  it('submit calls createHotel with the form payload in create mode', () => {
    createComponent(null);
    component.form.patchValue({
      name: 'New Hotel',
      slug: 'new-hotel',
      description: 'desc',
      city: 'Antalya',
      country: 'Turkey',
      address: 'addr',
      latitude: 1,
      longitude: 1,
    });

    component.submit();

    expect(adminHotelsServiceStub.createHotel).toHaveBeenCalledTimes(1);
    const payload = adminHotelsServiceStub.createHotel.mock.calls[0][0];
    expect(payload.name).toBe('New Hotel');
    expect(payload.slug).toBe('new-hotel');
  });

  it('submit calls updateHotel with the hotel id in edit mode', () => {
    createComponent('abc');

    component.submit();

    expect(adminHotelsServiceStub.updateHotel).toHaveBeenCalledTimes(1);
    expect(adminHotelsServiceStub.updateHotel.mock.calls[0][0]).toBe('abc');
  });

  it('toggleAmenity adds then removes a slug from the amenitySlugs control', () => {
    createComponent(null);

    component.toggleAmenity('wifi');
    expect(component.form.get('amenitySlugs')?.value).toEqual(['wifi']);

    component.toggleAmenity('wifi');
    expect(component.form.get('amenitySlugs')?.value).toEqual([]);
  });
});
