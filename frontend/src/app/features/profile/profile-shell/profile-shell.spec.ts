import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { ProfileShell } from './profile-shell';

describe('ProfileShell', () => {
  let fixture: ComponentFixture<ProfileShell>;
  let router: Router;

  function loginAs(displayName: string, firstName?: string): void {
    localStorage.setItem('vebtur_token', 'fake-token');
    localStorage.setItem(
      'vebtur_user',
      JSON.stringify({ email: 'user@vebtur.local', displayName, firstName, roles: ['Customer'] }),
    );
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

  it('renders links to every account and travel destination', () => {
    loginAs('Jane Guest');
    createFixture();

    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('a.account__nav-link'));
    const byHref = Object.fromEntries(links.map((l) => [l.getAttribute('href'), l.textContent?.trim()]));

    expect(byHref['/profile']).toContain('Personal Info');
    expect(byHref['/profile/security']).toContain('Account Security');
    expect(byHref['/favorites']).toContain('My Favorites');
    expect(byHref['/my-reservations']).toContain('My Reservations');
  });

  it('greets the signed-in user, preferring the first name over the display name', () => {
    loginAs('Jane Guest', 'Jane');
    createFixture();

    expect(fixture.nativeElement.querySelector('.account__greeting').textContent).toContain('Jane');
  });

  it('renders a router outlet for the active child route', () => {
    loginAs('Jane Guest');
    createFixture();

    expect(fixture.nativeElement.querySelector('router-outlet')).toBeTruthy();
  });

  it('clears the session and navigates home when "Log out" is clicked', () => {
    loginAs('Jane Guest');
    createFixture();

    fixture.nativeElement.querySelector('.account__logout').click();

    expect(localStorage.getItem('vebtur_token')).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });
});
