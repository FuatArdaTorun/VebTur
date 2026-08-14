import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AuthService } from './auth.service';
import { LoginResponse } from './auth.models';

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
    service.login({ email: 'admin@vebtur.local', password: 'secret' }).subscribe((r) => (received = r));

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/login'));
    expect(req.request.method).toBe('POST');
    req.flush(response);

    expect(received).toEqual(response);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.displayName).toBe('VebTur Admin');
    expect(service.getToken()).toBe('fake-jwt-token');
    expect(service.hasRole('Admin')).toBe(true);
    expect(service.hasRole('Customer')).toBe(false);
    expect(localStorage.getItem('vebtur_token')).toBe('fake-jwt-token');
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

    expect(received).toEqual(response);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.hasRole('Customer')).toBe(true);
    expect(service.hasRole('Admin')).toBe(false);
    expect(localStorage.getItem('vebtur_token')).toBe('fake-jwt-token');
  });

  it('logout clears stored token/user and flips isAuthenticated back to false', () => {
    const service = createService();

    service.login({ email: 'admin@vebtur.local', password: 'secret' }).subscribe();
    httpMock.expectOne((r) => r.url.endsWith('/api/v1/auth/login')).flush({
      token: 't',
      expiresAtUtc: new Date().toISOString(),
      email: 'admin@vebtur.local',
      displayName: 'Admin',
      roles: ['Admin'],
    } satisfies LoginResponse);
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
});
