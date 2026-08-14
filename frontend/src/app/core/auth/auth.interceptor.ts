import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Attaches the bearer token to every request whenever one is stored — harmless on public
 * endpoints (they simply ignore it), required for customer-authenticated routes like
 * /api/v1/reservation-requests/mine/* that aren't under /api/v1/admin/. A 401 only ever happens
 * on an endpoint that actually enforces [Authorize], so treating any 401 as "log out and
 * redirect to sign in" is correct regardless of URL — not just for admin calls.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.logout();
        router.navigate(['/login']);
      }

      return throwError(() => error);
    }),
  );
};
