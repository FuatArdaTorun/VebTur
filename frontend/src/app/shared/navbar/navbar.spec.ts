import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';

import { Navbar } from './navbar';

// A wildcard target so a real click on a routerLink (e.g. the mobile-menu-closes-on-navigation
// test) resolves instead of rejecting with "Cannot match any routes".
@Component({ template: '' })
class BlankRouteStub {}

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;
  let httpMock: HttpTestingController;

  async function createFixture(): Promise<void> {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [provideRouter([{ path: '**', component: BlankRouteStub }]), provideHttpClient(), provideHttpClientTesting()],
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

  /** A logged-in Admin's constructor effect fires an AwaitingApproval fetch for the notification bell instead. */
  function flushPendingApprovalsRequest(): void {
    httpMock
      .expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests'))
      .flush({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });
  }

  function openMenu(): void {
    fixture.nativeElement.querySelector('.navbar__avatar').click();
    fixture.detectChanges();
  }

  function dropdownLinkTexts(): string[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.navbar__dropdown-item')).map((el) => (el as HTMLElement).textContent?.trim());
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

  it('shows Sign In / Sign Up links and no avatar when unauthenticated', () => {
    fixture.detectChanges();
    const links: string[] = Array.from(fixture.nativeElement.querySelectorAll('a')).map((a) => (a as HTMLAnchorElement).textContent?.trim());

    expect(links).toContain('Sign In');
    expect(links).toContain('Sign Up');
    expect(links).toContain('Help');
    expect(fixture.nativeElement.querySelector('.navbar__avatar')).toBeNull();
  });

  it('toggles the mobile menu open and closed', () => {
    fixture.detectChanges();
    const toggle: HTMLButtonElement = fixture.nativeElement.querySelector('.navbar__menu-toggle');
    const links: HTMLElement = fixture.nativeElement.querySelector('.navbar__links');
    expect(links.classList.contains('navbar__links--open')).toBe(false);
    expect(toggle.getAttribute('aria-expanded')).toBe('false');

    toggle.click();
    fixture.detectChanges();
    expect(links.classList.contains('navbar__links--open')).toBe(true);
    expect(toggle.getAttribute('aria-expanded')).toBe('true');

    toggle.click();
    fixture.detectChanges();
    expect(links.classList.contains('navbar__links--open')).toBe(false);
  });

  it('closes the mobile menu when a link inside it is clicked', () => {
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.navbar__menu-toggle').click();
    fixture.detectChanges();

    const hotelsLink: HTMLAnchorElement = Array.from(fixture.nativeElement.querySelectorAll('a')).find(
      (a) => (a as HTMLAnchorElement).textContent?.trim() === 'Hotels',
    ) as HTMLAnchorElement;
    hotelsLink.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.navbar__links').classList.contains('navbar__links--open')).toBe(false);
  });

  it('closes the mobile menu when clicking outside it', () => {
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.navbar__menu-toggle').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.navbar__links').classList.contains('navbar__links--open')).toBe(true);

    document.body.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.navbar__links').classList.contains('navbar__links--open')).toBe(false);
  });

  it('shows an avatar with the display-name initial for a logged-in Customer, closed by default', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();
    flushMineRequest();

    const avatar: HTMLButtonElement = fixture.nativeElement.querySelector('.navbar__avatar');
    expect(avatar.textContent?.trim()).toBe('J');
    expect(fixture.nativeElement.querySelector('.navbar__dropdown')).toBeNull();
  });

  it('opens the dropdown on avatar click, showing the greeting, My Favorites and My Reservations, but no Admin Panel', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();
    flushMineRequest();
    openMenu();

    expect(fixture.nativeElement.querySelector('.navbar__dropdown-greeting').textContent).toContain('Jane Guest');
    const items = dropdownLinkTexts();
    expect(items).toContain('My Favorites');
    expect(items).toContain('My Reservations');
    expect(items).toContain('Personal Info');
    expect(items).toContain('Account Security');
    expect(items).not.toContain('Admin Panel');
  });

  it('shows a Help link to a logged-in Customer', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();
    flushMineRequest();

    const links: string[] = Array.from(fixture.nativeElement.querySelectorAll('a')).map((a) => (a as HTMLAnchorElement).textContent?.trim());
    expect(links).toContain('Help');
  });

  it('hides the Help link from a logged-in Admin — the admin panel is the support team, not a customer needing to contact one', async () => {
    loginAs(['Admin']);
    await createFixture();
    fixture.detectChanges();
    flushPendingApprovalsRequest();

    const links: string[] = Array.from(fixture.nativeElement.querySelectorAll('a')).map((a) => (a as HTMLAnchorElement).textContent?.trim());
    expect(links).not.toContain('Help');
  });

  it('shows a top-level Dashboard link next to Hotels for a logged-in Admin', async () => {
    loginAs(['Admin']);
    await createFixture();
    fixture.detectChanges();
    flushPendingApprovalsRequest();

    const link = fixture.nativeElement.querySelector('.navbar__link-group a[href="/admin/dashboard"]') as HTMLAnchorElement;
    expect(link?.textContent?.trim()).toBe('Dashboard');
  });

  it('does not show the Dashboard link to a logged-in Customer', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();
    flushMineRequest();

    const links: string[] = Array.from(fixture.nativeElement.querySelectorAll('.navbar__link-group a')).map(
      (a) => (a as HTMLAnchorElement).textContent?.trim(),
    );
    expect(links).not.toContain('Dashboard');
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

    expect(fixture.nativeElement.querySelector('.navbar__avatar').textContent?.trim()).toBe('J');
    openMenu();
    expect(fixture.nativeElement.querySelector('.navbar__dropdown-greeting').textContent).toContain('Jane');
  });

  it('shows no Management/Admin Panel dropdown group for a logged-in Admin — the top-level navbar Dashboard link covers that now, but no My Favorites/My Reservations either', async () => {
    loginAs(['Admin']);
    await createFixture();
    fixture.detectChanges();
    flushPendingApprovalsRequest();
    openMenu();

    const items = dropdownLinkTexts();
    expect(items).not.toContain('Admin Panel');
    expect(items).not.toContain('My Reservations');
    expect(items).not.toContain('My Favorites');
  });

  it('fetches AwaitingApproval reservations for the bell (not customer reservation-status updates) for a logged-in Admin', async () => {
    loginAs(['Admin']);
    await createFixture();
    fixture.detectChanges();

    httpMock.expectNone((r) => r.url.endsWith('/api/v1/reservation-requests/mine'));
    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/admin/reservation-requests'));
    expect(req.request.params.getAll('status')).toEqual(['AwaitingApproval']);
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

  it('closes the dropdown when clicking outside it', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();
    flushMineRequest();
    openMenu();
    expect(fixture.nativeElement.querySelector('.navbar__dropdown')).not.toBeNull();

    document.body.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.navbar__dropdown')).toBeNull();
  });

  it('logs out and navigates home when Log out is clicked', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();
    flushMineRequest();
    openMenu();

    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    const logoutButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('.navbar__dropdown-item')).find(
      (el) => (el as HTMLElement).textContent?.trim() === 'Log out',
    ) as HTMLButtonElement;
    logoutButton.click();
    fixture.detectChanges();

    expect(localStorage.getItem('vebtur_token')).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith('/');
  });
});
