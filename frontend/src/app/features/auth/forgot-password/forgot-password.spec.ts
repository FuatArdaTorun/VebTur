import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { ForgotPassword } from './forgot-password';
import { AuthService } from '../../../core/auth/auth.service';

describe('ForgotPassword', () => {
  let fixture: ComponentFixture<ForgotPassword>;
  let component: ForgotPassword;
  let authServiceStub: { forgotPassword: ReturnType<typeof vi.fn> };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [ForgotPassword],
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceStub }],
    });

    fixture = TestBed.createComponent(ForgotPassword);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('does not call the service when the email is invalid', () => {
    authServiceStub = { forgotPassword: vi.fn() };
    createComponent();

    component['form'].setValue({ email: 'not-an-email' });
    component['submit']();

    expect(authServiceStub.forgotPassword).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('stores the demo reset link when the email is registered', () => {
    authServiceStub = {
      forgotPassword: vi.fn().mockReturnValue(
        of({ message: 'If an account exists for that email, password reset instructions have been sent.', demoResetLink: 'http://localhost:4200/reset-password?email=a%40b.com&token=abc' }),
      ),
    };
    createComponent();

    component['form'].setValue({ email: 'a@b.com' });
    component['submit']();

    expect(component['result']()?.demoResetLink).toBe('http://localhost:4200/reset-password?email=a%40b.com&token=abc');
    expect(component['loading']()).toBe(false);
  });

  it('shows the same generic message with no link when the email is unknown', () => {
    authServiceStub = {
      forgotPassword: vi.fn().mockReturnValue(
        of({ message: 'If an account exists for that email, password reset instructions have been sent.', demoResetLink: null }),
      ),
    };
    createComponent();

    component['form'].setValue({ email: 'nobody@example.com' });
    component['submit']();

    expect(component['result']()?.message).toBe('If an account exists for that email, password reset instructions have been sent.');
    expect(component['result']()?.demoResetLink).toBeNull();
  });

  it('surfaces a backend error and stops loading on failure', () => {
    const httpError = new HttpErrorResponse({ status: 400, error: { errors: { Email: ['Email is not valid.'] } } });
    authServiceStub = { forgotPassword: vi.fn().mockReturnValue(throwError(() => httpError)) };
    createComponent();

    component['form'].setValue({ email: 'a@b.com' });
    component['submit']();

    expect(component['error']()).toBe('Email is not valid.');
    expect(component['loading']()).toBe(false);
    expect(component['result']()).toBeNull();
  });
});
