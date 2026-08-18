import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';

import { Navbar } from './navbar';

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;
  let httpMock: HttpTestingController;

  async function createFixture(): Promise<void> {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Navbar);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  }

  function loginAs(roles: string[]): void {
    localStorage.setItem('vebtur_token', 'fake-token');
    localStorage.setItem(
      'vebtur_user',
      JSON.stringify({ email: 'user@vebtur.local', displayName: 'Jane Guest', roles }),
    );
  }

  /** A logged-in Customer's constructor effect fires a "mine" fetch for the notification bell. */
  function flushMineRequest(): void {
    httpMock.expectOne((r) => r.url.endsWith('/api/v1/reservation-requests/mine')).flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
  }

  beforeEach(async () => {
    await createFixture();
  });

  afterEach(() => {
    localStorage.clear();
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows Sign In / Sign Up links and no logout button when unauthenticated', () => {
    fixture.detectChanges();
    const links: string[] = Array.from(fixture.nativeElement.querySelectorAll('a')).map((a) => (a as HTMLAnchorElement).textContent?.trim());

    expect(links).toContain('Sign In');
    expect(links).toContain('Sign Up');
    expect(links).not.toContain('Admin Panel');
    expect(fixture.nativeElement.querySelector('.navbar__logout')).toBeNull();
  });

  it('shows the display name, a logout button, and My Reservations, but no Admin Panel link, for a logged-in Customer', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();
    flushMineRequest();

    const links: string[] = Array.from(fixture.nativeElement.querySelectorAll('a')).map((a) => (a as HTMLAnchorElement).textContent?.trim());

    expect(fixture.nativeElement.querySelector('.navbar__user').textContent.trim()).toBe('Jane Guest');
    expect(fixture.nativeElement.querySelector('.navbar__logout')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.navbar__admin-link')).toBeNull();
    expect(links).toContain('My Reservations');
  });

  it('prefers the first name over the display name in the greeting when both are set', async () => {
    localStorage.setItem('vebtur_token', 'fake-token');
    localStorage.setItem(
      'vebtur_user',
      JSON.stringify({ email: 'user@vebtur.local', displayName: 'Jane Guest', firstName: 'Jane', roles: ['Customer'] }),
    );
    await createFixture();
    fixture.detectChanges();
    flushMineRequest();

    expect(fixture.nativeElement.querySelector('.navbar__user').textContent.trim()).toBe('Jane');
  });

  it('shows an Admin Panel link to /admin/hotels for a logged-in Admin, but no My Reservations link', async () => {
    loginAs(['Admin']);
    await createFixture();
    fixture.detectChanges();

    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('.navbar__admin-link');
    expect(link.textContent?.trim()).toBe('Admin Panel');
    expect(link.getAttribute('href')).toBe('/admin/hotels');

    const links: string[] = Array.from(fixture.nativeElement.querySelectorAll('a')).map((a) => (a as HTMLAnchorElement).textContent?.trim());
    expect(links).not.toContain('My Reservations');
  });

  it('does not fetch reservation status updates for a logged-in Admin', async () => {
    loginAs(['Admin']);
    await createFixture();
    fixture.detectChanges();

    httpMock.expectNone((r) => r.url.endsWith('/api/v1/reservation-requests/mine'));
  });

  it('only surfaces reservations with a decided status (Confirmed/Rejected/Cancelled), not AwaitingApproval', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();

    httpMock.expectOne((r) => r.url.endsWith('/api/v1/reservation-requests/mine')).flush({
      items: [
        { id: 'awaiting-1', hotelName: 'Awaiting Hotel', referenceNumber: 'VEB-AWAITNG1', status: 'AwaitingApproval' },
        { id: 'confirmed-1', hotelName: 'Confirmed Hotel', referenceNumber: 'VEB-CONFRM1', status: 'Confirmed' },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 2,
      totalPages: 1,
    });

    expect(component['recentStatusChanges']()).toEqual([
      {
        id: 'confirmed-1',
        title: 'Confirmed Hotel — Confirmed',
        subtitle: 'Reservation VEB-CONFRM1: see details',
        routerLink: ['/my-reservations', 'confirmed-1'],
      },
    ]);
  });

  it('logs out and navigates home when the logout button is clicked', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();
    flushMineRequest();

    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    fixture.nativeElement.querySelector('.navbar__logout').click();
    fixture.detectChanges();

    expect(localStorage.getItem('vebtur_token')).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith('/');
  });
});
