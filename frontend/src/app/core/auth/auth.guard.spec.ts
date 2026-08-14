import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';

import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  function setup(isAuthenticated: boolean, hasAdminRole: boolean) {
    const authServiceStub = {
      isAuthenticated: () => isAuthenticated,
      hasRole: (role: string) => (role === 'Admin' ? hasAdminRole : false),
    };

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceStub }],
    });
  }

  function runGuard(url: string) {
    return TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );
  }

  it('allows navigation when authenticated with the Admin role', () => {
    setup(true, true);

    expect(runGuard('/admin/hotels')).toBe(true);
  });

  it('redirects to /login with a returnUrl when not authenticated', () => {
    setup(false, false);

    const result = runGuard('/admin/hotels') as UrlTree;

    expect(result).toBeInstanceOf(UrlTree);
    expect(result.toString()).toContain('/login');
    expect(result.toString()).toContain('returnUrl');
    expect(result.toString()).toContain(encodeURIComponent('/admin/hotels'));
  });

  it('redirects to /login when authenticated but lacking the Admin role', () => {
    // Proves the guard checks the role claim, not merely "is some user logged in" — the same
    // property AdminHotels_WithNonAdminRoleToken_ReturnsForbidden checks on the backend.
    setup(true, false);

    const result = runGuard('/admin/hotels') as UrlTree;

    expect(result).toBeInstanceOf(UrlTree);
    expect(result.toString()).toContain('/login');
  });
});
