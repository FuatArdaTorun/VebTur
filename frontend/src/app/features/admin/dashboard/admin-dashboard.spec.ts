import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AdminDashboard } from './admin-dashboard';
import { AdminDashboardService } from './admin-dashboard.service';

describe('AdminDashboard', () => {
  let fixture: ComponentFixture<AdminDashboard>;
  let component: AdminDashboard;
  let dashboardServiceStub: { getSummary: ReturnType<typeof vi.fn> };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [AdminDashboard],
      providers: [provideRouter([]), { provide: AdminDashboardService, useValue: dashboardServiceStub }],
    });

    fixture = TestBed.createComponent(AdminDashboard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads the summary on init and stops loading', () => {
    dashboardServiceStub = {
      getSummary: vi.fn().mockReturnValue(
        of({ awaitingApprovalReservationsCount: 3, activeHotelsCount: 18, newSupportMessagesCount: 2, totalReviewsCount: 56 }),
      ),
    };
    createComponent();

    expect(component['loading']()).toBe(false);
    expect(component['error']()).toBe(false);
    expect(component['summary']()).toEqual({
      awaitingApprovalReservationsCount: 3,
      activeHotelsCount: 18,
      newSupportMessagesCount: 2,
      totalReviewsCount: 56,
    });
  });

  it('renders each count in its tile', () => {
    dashboardServiceStub = {
      getSummary: vi.fn().mockReturnValue(
        of({ awaitingApprovalReservationsCount: 3, activeHotelsCount: 18, newSupportMessagesCount: 2, totalReviewsCount: 56 }),
      ),
    };
    createComponent();

    const values: string[] = Array.from(fixture.nativeElement.querySelectorAll('.stat-tile__value')).map(
      (el) => (el as HTMLElement).textContent?.trim(),
    );
    expect(values).toEqual(['3', '2', '18', '56']);
  });

  it('sets error and stops loading when the summary fails to load', () => {
    dashboardServiceStub = { getSummary: vi.fn().mockReturnValue(throwError(() => new Error('network error'))) };
    createComponent();

    expect(component['error']()).toBe(true);
    expect(component['loading']()).toBe(false);
    expect(component['summary']()).toBeNull();
  });
});
