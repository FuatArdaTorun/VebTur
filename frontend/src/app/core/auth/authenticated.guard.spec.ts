import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';

import { authenticatedGuard } from './authenticated.guard';
import { AuthService } from './auth.service';

describe('authenticatedGuard', () => {
  function setup(isAuthenticated: boolean) {
    const authServiceStub = { isAuthenticated: () => isAuthenticated };

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceStub }],
    });
  }

  function runGuard(url: string) {
    return TestBed.runInInjectionContext(() =>
      authenticatedGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );
  }

  it('allows navigation for any signed-in user, regardless of role', () => {
    setup(true);

    expect(runGuard('/my-reservations')).toBe(true);
  });

  it('redirects to /login with a returnUrl when not authenticated', () => {
    setup(false);

    const result = runGuard('/my-reservations') as UrlTree;

    expect(result).toBeInstanceOf(UrlTree);
    expect(result.toString()).toContain('/login');
    expect(result.toString()).toContain(encodeURIComponent('/my-reservations'));
  });
});
