import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { ProfileShell } from './profile-shell';

describe('ProfileShell', () => {
  let fixture: ComponentFixture<ProfileShell>;
  let router: Router;

  function loginAs(roles: string[], firstName?: string): void {
    localStorage.setItem('vebtur_token', 'fake-token');
    localStorage.setItem('vebtur_user', JSON.stringify({ email: 'user@vebtur.local', displayName: 'Jane Guest', firstName, roles }));
  }

  function createFixture(): void {
    TestBed.configureTestingModule({
      imports: [ProfileShell],
      providers: [provideRouter([])],
    });

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    fixture = TestBed.createComponent(ProfileShell);
    fixture.detectChanges();
  }

  afterEach(() => localStorage.clear());

  it('shows the signed-in user\'s name in the brand link, preferring the first name over the display name', () => {
    loginAs(['Customer'], 'Jane');
    createFixture();

    expect(fixture.nativeElement.querySelector('.account__brand').textContent).toContain('Jane');
  });

  it('falls back to the display name when no first name is set', () => {
    loginAs(['Customer']);
    createFixture();

    expect(fixture.nativeElement.querySelector('.account__brand').textContent).toContain('Jane Guest');
  });

  it('renders links to every account and travel destination for a Customer, with no Management section', () => {
    loginAs(['Customer']);
    createFixture();

    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('a.account__nav-link'));
    const byHref = Object.fromEntries(links.map((l) => [l.getAttribute('href'), l.textContent?.trim()]));

    expect(byHref['/profile']).toContain('Personal Info');
    expect(byHref['/profile/security']).toContain('Account Security');
    expect(byHref['/favorites']).toContain('My Favorites');
    expect(byHref['/my-reservations']).toContain('My Reservations');
    expect(byHref['/admin/hotels']).toBeUndefined();
  });

  it('shows Admin Panel instead of Favorites/My Reservations for an Admin', () => {
    loginAs(['Admin']);
    createFixture();

    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('a.account__nav-link'));
    const byHref = Object.fromEntries(links.map((l) => [l.getAttribute('href'), l.textContent?.trim()]));

    expect(byHref['/profile']).toContain('Personal Info');
    expect(byHref['/profile/security']).toContain('Account Security');
    expect(byHref['/admin/hotels']).toContain('Admin Panel');
    expect(byHref['/favorites']).toBeUndefined();
    expect(byHref['/my-reservations']).toBeUndefined();
  });

  it('renders a router outlet for the active child route', () => {
    loginAs(['Customer']);
    createFixture();

    expect(fixture.nativeElement.querySelector('router-outlet')).toBeTruthy();
  });

  it('clears the session and navigates home when "Log out" is clicked', () => {
    loginAs(['Customer']);
    createFixture();

    fixture.nativeElement.querySelector('.account__logout').click();

    expect(localStorage.getItem('vebtur_token')).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });
});
