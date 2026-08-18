import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { ProfileSecurity } from './profile-security';
import { AuthService } from '../../../core/auth/auth.service';

describe('ProfileSecurity', () => {
  let fixture: ComponentFixture<ProfileSecurity>;
  let component: ProfileSecurity;
  let authServiceStub: { changePassword: ReturnType<typeof vi.fn> };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [ProfileSecurity],
      providers: [{ provide: AuthService, useValue: authServiceStub }],
    });

    fixture = TestBed.createComponent(ProfileSecurity);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('does not call changePassword when the new passwords do not match', () => {
    authServiceStub = { changePassword: vi.fn() };
    createComponent();
    component['form'].setValue({ currentPassword: 'old', newPassword: 'NewPassw0rd1', confirmPassword: 'Different1' });

    component['submit']();

    expect(authServiceStub.changePassword).not.toHaveBeenCalled();
    expect(component['form'].errors?.['passwordMismatch']).toBe(true);
  });

  it('changes the password, shows a success message, and clears the form', () => {
    authServiceStub = { changePassword: vi.fn().mockReturnValue(of(undefined)) };
    createComponent();
    component['form'].setValue({ currentPassword: 'old', newPassword: 'NewPassw0rd1', confirmPassword: 'NewPassw0rd1' });

    component['submit']();

    expect(authServiceStub.changePassword).toHaveBeenCalledWith({ currentPassword: 'old', newPassword: 'NewPassw0rd1' });
    expect(component['saved']()).toBe(true);
    expect(component['submitting']()).toBe(false);
    expect(component['form'].controls.newPassword.value).toBe('');
  });

  it('surfaces an error when the current password is wrong', () => {
    authServiceStub = {
      changePassword: vi.fn().mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 400, error: { errors: { PasswordMismatch: ['Incorrect password.'] } } })),
      ),
    };
    createComponent();
    component['form'].setValue({ currentPassword: 'wrong', newPassword: 'NewPassw0rd1', confirmPassword: 'NewPassw0rd1' });

    component['submit']();

    expect(component['error']()).toBe('Incorrect password.');
    expect(component['submitting']()).toBe(false);
  });

  it('does not call changePassword when the form is invalid', () => {
    authServiceStub = { changePassword: vi.fn() };
    createComponent();

    component['submit']();

    expect(authServiceStub.changePassword).not.toHaveBeenCalled();
    expect(component['form'].controls.currentPassword.touched).toBe(true);
  });
});
