import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { AdminShell } from './admin-shell';
import { AuthService } from '../../../core/auth/auth.service';

describe('AdminShell', () => {
  let fixture: ComponentFixture<AdminShell>;
  let component: AdminShell;
  let authServiceStub: { currentUser: () => unknown; logout: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(() => {
    authServiceStub = { currentUser: () => ({ displayName: 'VebTur Admin' }), logout: vi.fn() };

    TestBed.configureTestingModule({
      imports: [AdminShell],
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceStub }],
    });

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(AdminShell);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('logs out and navigates to /login', () => {
    component['logout']();

    expect(authServiceStub.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
