import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { ContactSupport } from './contact-support';
import { HelpService } from '../help.service';
import { AuthService } from '../../../core/auth/auth.service';
import { CurrentUserResponse } from '../../../core/auth/auth.models';

describe('ContactSupport', () => {
  let fixture: ComponentFixture<ContactSupport>;
  let component: ContactSupport;
  let helpServiceStub: { sendMessage: ReturnType<typeof vi.fn> };
  let authServiceStub: { isAuthenticated: ReturnType<typeof vi.fn>; getProfile: ReturnType<typeof vi.fn> };

  function createComponent(isAuthenticated = false): void {
    helpServiceStub = { sendMessage: vi.fn().mockReturnValue(of({ id: 'msg-1', createdAtUtc: '2026-08-20T00:00:00Z' })) };
    authServiceStub = {
      isAuthenticated: vi.fn().mockReturnValue(isAuthenticated),
      getProfile: vi.fn().mockReturnValue(
        of({ email: 'jane@example.com', displayName: 'Jane Guest' } as CurrentUserResponse),
      ),
    };

    TestBed.configureTestingModule({
      imports: [ContactSupport],
      providers: [
        { provide: HelpService, useValue: helpServiceStub },
        { provide: AuthService, useValue: authServiceStub },
      ],
    });

    fixture = TestBed.createComponent(ContactSupport);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('should create', () => {
    createComponent();
    expect(component).toBeTruthy();
  });

  it('does not submit an invalid form', () => {
    createComponent();

    component['submit']();

    expect(helpServiceStub.sendMessage).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('submits a valid form and shows the success state', () => {
    createComponent();
    component['form'].setValue({ name: 'Jane Guest', email: 'jane@example.com', subject: 'Question', message: 'Hello there.' });

    component['submit']();

    expect(helpServiceStub.sendMessage).toHaveBeenCalledWith({
      name: 'Jane Guest',
      email: 'jane@example.com',
      subject: 'Question',
      message: 'Hello there.',
    });
    expect(component['submitted']()).toBe(true);
  });

  it('surfaces a server error without marking the message as submitted', () => {
    createComponent();
    helpServiceStub.sendMessage.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400 })));
    component['form'].setValue({ name: 'Jane Guest', email: 'jane@example.com', subject: 'Question', message: 'Hello there.' });

    component['submit']();

    expect(component['submitted']()).toBe(false);
    expect(component['submitError']()).toBeTruthy();
  });

  it('does not show the "use my saved profile info" checkbox when unauthenticated', () => {
    createComponent(false);

    expect(fixture.nativeElement.querySelector('.contact-support__checkbox')).toBeNull();
  });

  it('fills in name/email from the profile when the checkbox is checked, and clears them when unchecked', () => {
    createComponent(true);

    component['onUseMyInfoChange'](true);
    expect(authServiceStub.getProfile).toHaveBeenCalled();
    expect(component['form'].value.name).toBe('Jane Guest');
    expect(component['form'].value.email).toBe('jane@example.com');

    component['onUseMyInfoChange'](false);
    expect(component['form'].value.name).toBe('');
    expect(component['form'].value.email).toBe('');
  });
});
