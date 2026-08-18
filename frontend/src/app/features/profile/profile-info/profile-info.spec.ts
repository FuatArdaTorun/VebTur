import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { ProfileInfo } from './profile-info';
import { AuthService } from '../../../core/auth/auth.service';
import { CurrentUserResponse } from '../../../core/auth/auth.models';

function buildProfile(overrides: Partial<CurrentUserResponse> = {}): CurrentUserResponse {
  return {
    id: 'u1',
    email: 'guest@example.com',
    userName: 'guest@example.com',
    displayName: 'Guest User',
    phoneNumber: null,
    firstName: null,
    lastName: null,
    gender: null,
    dateOfBirth: null,
    roles: ['Customer'],
    ...overrides,
  };
}

describe('ProfileInfo', () => {
  let fixture: ComponentFixture<ProfileInfo>;
  let component: ProfileInfo;
  let authServiceStub: { getProfile: ReturnType<typeof vi.fn>; updateProfile: ReturnType<typeof vi.fn> };

  function createComponent(): void {
    TestBed.configureTestingModule({
      imports: [ProfileInfo],
      providers: [{ provide: AuthService, useValue: authServiceStub }],
    });

    fixture = TestBed.createComponent(ProfileInfo);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('loads the profile on init and populates the form', () => {
    authServiceStub = {
      getProfile: vi.fn().mockReturnValue(
        of(
          buildProfile({
            userName: 'selin_y',
            phoneNumber: '+90 555 111 22 33',
            firstName: 'Selin',
            lastName: 'Yildiz',
            gender: 'Female',
            dateOfBirth: '1995-06-15',
          }),
        ),
      ),
      updateProfile: vi.fn(),
    };

    createComponent();

    expect(component['loading']()).toBe(false);
    expect(component['email']()).toBe('guest@example.com');
    expect(component['form'].controls.userName.value).toBe('selin_y');
    expect(component['form'].controls.phoneNumber.value).toBe('+90 555 111 22 33');
    expect(component['form'].controls.firstName.value).toBe('Selin');
    expect(component['form'].controls.lastName.value).toBe('Yildiz');
    expect(component['form'].controls.gender.value).toBe('Female');
    expect(component['form'].controls.dateOfBirth.value).toBe('1995-06-15');
  });

  it('shows an error state when the profile fails to load', () => {
    authServiceStub = { getProfile: vi.fn().mockReturnValue(throwError(() => new Error('nope'))), updateProfile: vi.fn() };

    createComponent();

    expect(component['loadError']()).toBe(true);
    expect(component['loading']()).toBe(false);
  });

  it('does not call updateProfile when the form is invalid', () => {
    authServiceStub = { getProfile: vi.fn().mockReturnValue(of(buildProfile())), updateProfile: vi.fn() };
    createComponent();
    component['form'].patchValue({ userName: '' });

    component['submit']();

    expect(authServiceStub.updateProfile).not.toHaveBeenCalled();
    expect(component['form'].controls.userName.touched).toBe(true);
  });

  it('saves the full profile and shows a success message', () => {
    authServiceStub = {
      getProfile: vi.fn().mockReturnValue(of(buildProfile())),
      updateProfile: vi.fn().mockReturnValue(
        of(buildProfile({ userName: 'updated_name', phoneNumber: '+90 555 999 88 77', firstName: 'Ada', lastName: 'Lovelace' })),
      ),
    };
    createComponent();
    component['form'].patchValue({
      userName: 'updated_name',
      phoneNumber: '+90 555 999 88 77',
      firstName: 'Ada',
      lastName: 'Lovelace',
      gender: 'PreferNotToSay',
      dateOfBirth: '1990-01-01',
    });

    component['submit']();

    expect(authServiceStub.updateProfile).toHaveBeenCalledWith({
      userName: 'updated_name',
      phoneNumber: '+90 555 999 88 77',
      firstName: 'Ada',
      lastName: 'Lovelace',
      gender: 'PreferNotToSay',
      dateOfBirth: '1990-01-01',
    });
    expect(component['saved']()).toBe(true);
    expect(component['submitting']()).toBe(false);
  });

  it('surfaces the server message (e.g. duplicate username) when saving fails', () => {
    authServiceStub = {
      getProfile: vi.fn().mockReturnValue(of(buildProfile())),
      updateProfile: vi.fn().mockReturnValue(
        throwError(
          () =>
            new HttpErrorResponse({
              status: 400,
              error: { errors: { DuplicateUserName: ["Username 'taken' is already taken."] } },
            }),
        ),
      ),
    };
    createComponent();
    component['form'].patchValue({ userName: 'taken' });

    component['submit']();

    expect(component['submitError']()).toBe("Username 'taken' is already taken.");
    expect(component['submitting']()).toBe(false);
  });
});
