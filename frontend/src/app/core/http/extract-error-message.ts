import { HttpErrorResponse } from '@angular/common/http';

// Pulls the first message out of an ASP.NET Core ValidationProblem's `errors` object
// (`{ errors: { SomeKey: ["message"] } }`), regardless of what the key is named — used to
// surface FluentValidation/Identity error text (e.g. "Username 'x' is already taken.") verbatim.
export function extractErrorMessage(response: HttpErrorResponse, fallback: string): string {
  const errors = response.error?.errors;
  if (errors && typeof errors === 'object') {
    const firstMessage = Object.values(errors).flat()[0];
    if (typeof firstMessage === 'string') {
      return firstMessage;
    }
  }

  return fallback;
}
