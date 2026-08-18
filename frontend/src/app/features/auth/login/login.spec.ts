import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Login } from './login';
import { AuthService } from '../../../core/auth/auth.service';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let component: Login;
  let authServiceStub: { login: ReturnType<typeof vi.fn> };
  let router: Router;

  function createComponent(returnUrl: string | null = null): void {
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceStub },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(returnUrl ? { returnUrl } : {}) } },
        },
      ],
    });

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('does not call the service and marks fields touched when the form is invalid', () => {
    authServiceStub = { login: vi.fn() };
    createComponent();

    component['submit']();

    expect(authServiceStub.login).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('redirects an Admin to /admin/hotels after a successful login with no returnUrl', () => {
    authServiceStub = { login: vi.fn().mockReturnValue(of({ token: 't', expiresAtUtc: '', email: 'a@b.com', displayName: 'A', roles: ['Admin'] })) };
    createComponent();

    component['form'].setValue({ emailOrUsername: 'a@b.com', password: 'secret123' });
    component['submit']();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/admin/hotels');
  });

  it('redirects a non-Admin to / after a successful login with no returnUrl', () => {
    authServiceStub = { login: vi.fn().mockReturnValue(of({ token: 't', expiresAtUtc: '', email: 'a@b.com', displayName: 'A', roles: ['Customer'] })) };
    createComponent();

    component['form'].setValue({ emailOrUsername: 'a@b.com', password: 'secret123' });
    component['submit']();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('honors returnUrl over the role-based fallback', () => {
    authServiceStub = { login: vi.fn().mockReturnValue(of({ token: 't', expiresAtUtc: '', email: 'a@b.com', displayName: 'A', roles: ['Customer'] })) };
    createComponent('/hotels/some-hotel');

    component['form'].setValue({ emailOrUsername: 'a@b.com', password: 'secret123' });
    component['submit']();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/hotels/some-hotel');
  });

  it('sets an error and stops loading when login fails', () => {
    authServiceStub = { login: vi.fn().mockReturnValue(throwError(() => new Error('unauthorized'))) };
    createComponent();

    component['form'].setValue({ emailOrUsername: 'a@b.com', password: 'wrong' });
    component['submit']();

    expect(component['error']()).toBe(true);
    expect(component['loading']()).toBe(false);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
