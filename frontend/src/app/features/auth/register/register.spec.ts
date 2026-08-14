import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Register } from './register';
import { AuthService } from '../../../core/auth/auth.service';

describe('Register', () => {
  let fixture: ComponentFixture<Register>;
  let component: Register;
  let authServiceStub: { register: ReturnType<typeof vi.fn> };
  let router: Router;

  function createComponent(returnUrl: string | null = null): void {
    TestBed.configureTestingModule({
      imports: [Register],
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

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('does not call the service when the form is invalid (e.g. password too short)', () => {
    authServiceStub = { register: vi.fn() };
    createComponent();

    component['form'].setValue({ displayName: 'Jane', email: 'jane@example.com', password: 'short' });
    component['submit']();

    expect(authServiceStub.register).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('registers and redirects to / by default on success', () => {
    authServiceStub = {
      register: vi.fn().mockReturnValue(of({ token: 't', expiresAtUtc: '', email: 'jane@example.com', displayName: 'Jane', roles: ['Customer'] })),
    };
    createComponent();

    component['form'].setValue({ displayName: 'Jane', email: 'jane@example.com', password: 'Passw0rd' });
    component['submit']();

    expect(authServiceStub.register).toHaveBeenCalledWith({ displayName: 'Jane', email: 'jane@example.com', password: 'Passw0rd' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('honors returnUrl after a successful registration', () => {
    authServiceStub = {
      register: vi.fn().mockReturnValue(of({ token: 't', expiresAtUtc: '', email: 'jane@example.com', displayName: 'Jane', roles: ['Customer'] })),
    };
    createComponent('/hotels/some-hotel');

    component['form'].setValue({ displayName: 'Jane', email: 'jane@example.com', password: 'Passw0rd' });
    component['submit']();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/hotels/some-hotel');
  });

  it('surfaces the backend validation message (e.g. duplicate email) on failure', () => {
    const httpError = new HttpErrorResponse({
      status: 400,
      error: { errors: { DuplicateUserName: ["Email 'jane@example.com' is already taken."] } },
    });
    authServiceStub = { register: vi.fn().mockReturnValue(throwError(() => httpError)) };
    createComponent();

    component['form'].setValue({ displayName: 'Jane', email: 'jane@example.com', password: 'Passw0rd' });
    component['submit']();

    expect(component['error']()).toBe("Email 'jane@example.com' is already taken.");
    expect(component['loading']()).toBe(false);
  });

  it('falls back to a generic message when the error body has no structured errors', () => {
    const httpError = new HttpErrorResponse({ status: 500, error: null });
    authServiceStub = { register: vi.fn().mockReturnValue(throwError(() => httpError)) };
    createComponent();

    component['form'].setValue({ displayName: 'Jane', email: 'jane@example.com', password: 'Passw0rd' });
    component['submit']();

    expect(component['error']()).toBe('Could not create your account. Please check your details and try again.');
  });
});
