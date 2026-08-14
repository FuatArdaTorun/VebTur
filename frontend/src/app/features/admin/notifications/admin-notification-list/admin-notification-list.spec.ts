import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AdminNotificationList } from './admin-notification-list';
import { AdminNotificationsService } from '../admin-notifications.service';

describe('AdminNotificationList', () => {
  let fixture: ComponentFixture<AdminNotificationList>;
  let component: AdminNotificationList;
  let serviceStub: { getNotifications: ReturnType<typeof vi.fn> };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [AdminNotificationList],
      providers: [provideRouter([]), { provide: AdminNotificationsService, useValue: serviceStub }],
    });

    fixture = TestBed.createComponent(AdminNotificationList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads and displays notifications on init', () => {
    serviceStub = { getNotifications: vi.fn().mockReturnValue(of({ items: [{ id: 'n-1' }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })) };
    createComponent();

    expect(component['notifications']().length).toBe(1);
    expect(component['loading']()).toBe(false);
  });

  it('shows the error state when the fetch fails', () => {
    serviceStub = { getNotifications: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) };
    createComponent();

    expect(component['error']()).toBe(true);
  });

  it('starts with an empty search box and no filters on the initial fetch', () => {
    serviceStub = { getNotifications: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();

    expect(component['searchControl'].value).toBe('');
    expect(serviceStub.getNotifications).toHaveBeenCalledWith(expect.objectContaining({ status: undefined, search: undefined, page: 1 }));
  });

  it('applying the filter re-fetches with the selected status and typed search, resetting to page 1', () => {
    serviceStub = { getNotifications: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })) };
    createComponent();
    component['page'].set(3);

    component['statusControl'].setValue('Sent');
    component['searchControl'].setValue('VEB-1234');
    component['applyFilter']();

    expect(serviceStub.getNotifications).toHaveBeenLastCalledWith(expect.objectContaining({ status: 'Sent', search: 'VEB-1234', page: 1 }));
  });

  it('goToPage ignores out-of-range pages', () => {
    serviceStub = { getNotifications: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 2 })) };
    createComponent();
    const callsBefore = serviceStub.getNotifications.mock.calls.length;

    component['goToPage'](0);
    component['goToPage'](99);

    expect(serviceStub.getNotifications.mock.calls.length).toBe(callsBefore);
  });

  it('goToPage fetches the requested page when in range', () => {
    serviceStub = { getNotifications: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 2 })) };
    createComponent();

    component['goToPage'](2);

    expect(serviceStub.getNotifications).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 }));
  });
});
