import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let router: Router;
  let authServiceStub: { getToken: () => string | null; logout: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authServiceStub = { getToken: () => 'test-token', logout: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authServiceStub },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('attaches a Bearer token to any request when one is stored, including public endpoints', () => {
    httpClient.get('/api/v1/hotels').subscribe();

    const req = httpMock.expectOne('/api/v1/hotels');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    req.flush({});
  });

  it('attaches a Bearer token to admin API calls', () => {
    httpClient.get('/api/v1/admin/hotels').subscribe();

    const req = httpMock.expectOne('/api/v1/admin/hotels');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    req.flush({});
  });

  it('sends a request without a token unmodified when none is stored', () => {
    authServiceStub.getToken = () => null;

    httpClient.get('/api/v1/admin/hotels').subscribe();

    const req = httpMock.expectOne('/api/v1/admin/hotels');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('logs out and redirects to /login on a 401, regardless of URL', () => {
    const navigateSpy = vi.spyOn(router, 'navigate');

    httpClient.get('/api/v1/reservation-requests/mine').subscribe({ error: () => {} });
    httpMock.expectOne('/api/v1/reservation-requests/mine').flush({ message: 'unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(authServiceStub.logout).toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });

  it('does not log out on a non-401 error', () => {
    httpClient.get('/api/v1/admin/hotels').subscribe({ error: () => {} });
    httpMock.expectOne('/api/v1/admin/hotels').flush({ message: 'boom' }, { status: 500, statusText: 'Server Error' });

    expect(authServiceStub.logout).not.toHaveBeenCalled();
  });
});
