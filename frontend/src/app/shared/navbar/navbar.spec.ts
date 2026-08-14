import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';

import { Navbar } from './navbar';

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;

  async function createFixture(): Promise<void> {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Navbar);
    component = fixture.componentInstance;
  }

  function loginAs(roles: string[]): void {
    localStorage.setItem('vebtur_token', 'fake-token');
    localStorage.setItem(
      'vebtur_user',
      JSON.stringify({ email: 'user@vebtur.local', displayName: 'Jane Guest', roles }),
    );
  }

  beforeEach(async () => {
    await createFixture();
  });

  afterEach(() => {
    localStorage.clear();
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

    const links: string[] = Array.from(fixture.nativeElement.querySelectorAll('a')).map((a) => (a as HTMLAnchorElement).textContent?.trim());

    expect(fixture.nativeElement.querySelector('.navbar__user').textContent.trim()).toBe('Jane Guest');
    expect(fixture.nativeElement.querySelector('.navbar__logout')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.navbar__admin-link')).toBeNull();
    expect(links).toContain('My Reservations');
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

  it('logs out and navigates home when the logout button is clicked', async () => {
    loginAs(['Customer']);
    await createFixture();
    fixture.detectChanges();

    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    fixture.nativeElement.querySelector('.navbar__logout').click();
    fixture.detectChanges();

    expect(localStorage.getItem('vebtur_token')).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith('/');
  });
});
