import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { ResetPassword } from './reset-password';
import { AuthService } from '../../../core/auth/auth.service';

describe('ResetPassword', () => {
  let fixture: ComponentFixture<ResetPassword>;
  let component: ResetPassword;
  let authServiceStub: { resetPassword: ReturnType<typeof vi.fn> };

  function createComponent(queryParams: Record<string, string> = { email: 'a@b.com', token: 'abc-token' }): void {
    TestBed.configureTestingModule({
      imports: [ResetPassword],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceStub },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } } },
      ],
    });

    fixture = TestBed.createComponent(ResetPassword);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('treats a link missing the email or token as invalid', () => {
    authServiceStub = { resetPassword: vi.fn() };
    createComponent({});

    expect(component['linkIsValid']).toBe(false);
  });

  it('treats a link with both email and token as valid', () => {
    authServiceStub = { resetPassword: vi.fn() };
    createComponent();

    expect(component['linkIsValid']).toBe(true);
  });

  it('does not call the service when the passwords do not match', () => {
    authServiceStub = { resetPassword: vi.fn() };
    createComponent();

    component['form'].setValue({ newPassword: 'Passw0rd123', confirmPassword: 'Different123' });
    component['submit']();

    expect(authServiceStub.resetPassword).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('resets the password and shows the done state on success', () => {
    authServiceStub = { resetPassword: vi.fn().mockReturnValue(of(undefined)) };
    createComponent();

    component['form'].setValue({ newPassword: 'Passw0rd123', confirmPassword: 'Passw0rd123' });
    component['submit']();

    expect(authServiceStub.resetPassword).toHaveBeenCalledWith({ email: 'a@b.com', token: 'abc-token', newPassword: 'Passw0rd123' });
    expect(component['done']()).toBe(true);
    expect(component['submitting']()).toBe(false);
  });

  it('surfaces a backend error (e.g. expired token) and stays on the form', () => {
    const httpError = new HttpErrorResponse({ status: 400, error: { errors: { InvalidToken: ['Invalid token.'] } } });
    authServiceStub = { resetPassword: vi.fn().mockReturnValue(throwError(() => httpError)) };
    createComponent();

    component['form'].setValue({ newPassword: 'Passw0rd123', confirmPassword: 'Passw0rd123' });
    component['submit']();

    expect(component['error']()).toBe('Invalid token.');
    expect(component['done']()).toBe(false);
    expect(component['submitting']()).toBe(false);
  });
});
