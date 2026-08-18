import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ProfileShell } from './profile-shell';

describe('ProfileShell', () => {
  let fixture: ComponentFixture<ProfileShell>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProfileShell],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(ProfileShell);
    fixture.detectChanges();
  });

  it('renders a "Personal Info" link to /profile and an "Account Security" link to /profile/security', () => {
    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('a.account__nav-link'));

    expect(links).toHaveLength(2);
    expect(links[0].textContent).toContain('Personal Info');
    expect(links[0].getAttribute('href')).toBe('/profile');
    expect(links[1].textContent).toContain('Account Security');
    expect(links[1].getAttribute('href')).toBe('/profile/security');
  });

  it('renders a router outlet for the active child route', () => {
    expect(fixture.nativeElement.querySelector('router-outlet')).toBeTruthy();
  });
});
