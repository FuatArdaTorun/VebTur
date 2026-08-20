import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AuthService } from './auth.service';
import { CurrentUserResponse, LoginResponse } from './auth.models';

describe('AuthService', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    httpMock?.verify();
    localStorage.clear();
  });

  function createService(): AuthService {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  it('starts unauthenticated when nothing is stored', () => {
    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(service.getToken()).toBeNull();
  });

  /** login/register fire a follow-up GET /auth/me to refresh firstName etc. right away — see storeSession. */
  function flushProfileRequest(overrides: Partial<CurrentUserResponse> = {}): void {
    httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/me')).flush({
      id: 'u1',
      email: 'admin@vebtur.local',
      userName: 'admin@vebtur.local',
      displayName: 'VebTur Admin',
      phoneNumber: null,
      firstName: null,
      lastName: null,
      gender: null,
      dateOfBirth: null,
      roles: ['Admin'],
      ...overrides,
    } satisfies CurrentUserResponse);
  }

  it('login stores the token/user and flips isAuthenticated', () => {
    const service = createService();
    const response: LoginResponse = {
      token: 'fake-jwt-token',
      expiresAtUtc: new Date().toISOString(),
      email: 'admin@vebtur.local',
      displayName: 'VebTur Admin',
      roles: ['Admin'],
    };

    let received: unknown;
    service.login({ emailOrUsername: 'admin@vebtur.local', password: 'secret' }).subscribe((r) => (received = r));

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/login'));
    expect(req.request.method).toBe('POST');
    req.flush(response);
    flushProfileRequest();

    expect(received).toEqual(response);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.displayName).toBe('VebTur Admin');
    expect(service.getToken()).toBe('fake-jwt-token');
    expect(service.hasRole('Admin')).toBe(true);
    expect(service.hasRole('Customer')).toBe(false);
    expect(localStorage.getItem('vebtur_token')).toBe('fake-jwt-token');
  });

  it('login refreshes firstName from a follow-up /auth/me call, so the profile is accurate immediately — not only after visiting Personal Info', () => {
    const service = createService();

    service.login({ emailOrUsername: 'admin@vebtur.local', password: 'secret' }).subscribe();
    httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/login')).flush({
      token: 'fake-jwt-token',
      expiresAtUtc: new Date().toISOString(),
      email: 'admin@vebtur.local',
      displayName: 'Client',
      roles: ['Customer'],
    } satisfies LoginResponse);

    // Right after the login response, firstName is still unknown (not carried by LoginResponse).
    expect(service.currentUser()?.firstName).toBeNull();
    expect(service.currentUser()?.displayName).toBe('Client');

    flushProfileRequest({ displayName: 'Client', firstName: 'Arda', roles: ['Customer'] });

    expect(service.currentUser()?.firstName).toBe('Arda');
  });

  it('register stores the token/user and flips isAuthenticated, same as login', () => {
    const service = createService();
    const response: LoginResponse = {
      token: 'fake-jwt-token',
      expiresAtUtc: new Date().toISOString(),
      email: 'guest@example.com',
      displayName: 'Guest User',
      roles: ['Customer'],
    };

    let received: unknown;
    service.register({ email: 'guest@example.com', password: 'Passw0rd', displayName: 'Guest User' }).subscribe((r) => (received = r));

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/register'));
    expect(req.request.method).toBe('POST');
    req.flush(response);
    flushProfileRequest({ displayName: 'Guest User', roles: ['Customer'] });

    expect(received).toEqual(response);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.hasRole('Customer')).toBe(true);
    expect(service.hasRole('Admin')).toBe(false);
    expect(localStorage.getItem('vebtur_token')).toBe('fake-jwt-token');
  });

  it('logout clears stored token/user and flips isAuthenticated back to false', () => {
    const service = createService();

    service.login({ emailOrUsername: 'admin@vebtur.local', password: 'secret' }).subscribe();
    httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/login')).flush({
      token: 't',
      expiresAtUtc: new Date().toISOString(),
      email: 'admin@vebtur.local',
      displayName: 'Admin',
      roles: ['Admin'],
    } satisfies LoginResponse);
    flushProfileRequest();
    expect(service.isAuthenticated()).toBe(true);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(service.getToken()).toBeNull();
    expect(localStorage.getItem('vebtur_token')).toBeNull();
  });

  it('restores a previously logged-in user from localStorage on construction', () => {
    localStorage.setItem('vebtur_token', 'persisted-token');
    localStorage.setItem(
      'vebtur_user',
      JSON.stringify({ email: 'admin@vebtur.local', displayName: 'Admin', roles: ['Admin'] }),
    );

    const service = createService();

    expect(service.isAuthenticated()).toBe(true);
    expect(service.getToken()).toBe('persisted-token');
    expect(service.hasRole('Admin')).toBe(true);
  });

  it('getProfile fetches /auth/me and stores the phone number alongside the rest of the profile', () => {
    const service = createService();
    const response: CurrentUserResponse = {
      id: 'u1',
      email: 'guest@example.com',
      userName: 'guest@example.com',
      displayName: 'Guest User',
      phoneNumber: '+90 555 111 22 33',
      firstName: null,
      lastName: null,
      gender: null,
      dateOfBirth: null,
      roles: ['Customer'],
    };

    let received: unknown;
    service.getProfile().subscribe((r) => (received = r));

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/me'));
    expect(req.request.method).toBe('GET');
    req.flush(response);

    expect(received).toEqual(response);
    expect(service.currentUser()?.phoneNumber).toBe('+90 555 111 22 33');
  });

  it('updateProfile PUTs /auth/me and updates the stored user', () => {
    const service = createService();
    const response: CurrentUserResponse = {
      id: 'u1',
      email: 'guest@example.com',
      userName: 'updated_username',
      displayName: 'Guest User',
      phoneNumber: '+90 555 999 88 77',
      firstName: null,
      lastName: null,
      gender: null,
      dateOfBirth: null,
      roles: ['Customer'],
    };

    let received: unknown;
    service
      .updateProfile({
        userName: 'updated_username',
        phoneNumber: '+90 555 999 88 77',
        firstName: null,
        lastName: null,
        gender: null,
        dateOfBirth: null,
      })
      .subscribe((r) => (received = r));

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/me'));
    expect(req.request.method).toBe('PUT');
    req.flush(response);

    expect(received).toEqual(response);
    expect(service.currentUser()?.displayName).toBe('Guest User');
    expect(service.currentUser()?.phoneNumber).toBe('+90 555 999 88 77');
  });

  it('changePassword POSTs /auth/change-password', () => {
    const service = createService();

    let received: unknown;
    let completed = false;
    service.changePassword({ currentPassword: 'old', newPassword: 'NewPassw0rd1' }).subscribe({
      next: (r) => (received = r),
      complete: () => (completed = true),
    });

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/change-password'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ currentPassword: 'old', newPassword: 'NewPassw0rd1' });
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
    expect(received).toBeNull();
  });
});
