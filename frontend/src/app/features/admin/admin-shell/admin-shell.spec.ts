import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { AdminShell } from './admin-shell';

describe('AdminShell', () => {
  let fixture: ComponentFixture<AdminShell>;
  let component: AdminShell;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AdminShell],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });

    fixture = TestBed.createComponent(AdminShell);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('fetches pending-approval reservations (Pending + Sent) on init', () => {
    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests'));
    expect(req.request.params.getAll('status')).toEqual(['Pending', 'Sent']);

    req.flush({
      items: [{ id: 'res-1', hotelName: 'Test Hotel', referenceNumber: 'VEB-ABC12345', guestFullName: 'Jane Guest' }],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    });

    expect(component['pendingApprovals']()).toEqual([
      {
        id: 'res-1',
        title: 'Test Hotel — VEB-ABC12345',
        subtitle: 'Jane Guest · awaiting approval',
        routerLink: ['/admin/reservations', 'res-1'],
      },
    ]);
  });

  it('refetches when the bell is opened', () => {
    httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests')).flush({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });

    component['fetchPendingApprovals']();

    httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests')).flush({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });
  });
});
