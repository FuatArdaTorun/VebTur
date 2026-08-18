import { HttpErrorResponse } from '@angular/common/http';
import { extractErrorMessage } from './extract-error-message';

describe('extractErrorMessage', () => {
  it('returns the first message from a ValidationProblem errors object', () => {
    const response = new HttpErrorResponse({
      error: { errors: { UserName: ["Username 'x' is already taken."] } },
    });

    expect(extractErrorMessage(response, 'fallback')).toBe("Username 'x' is already taken.");
  });

  it('returns the first message when multiple fields have errors', () => {
    const response = new HttpErrorResponse({
      error: { errors: { Email: ['Email is required.'], Password: ['Password is too short.'] } },
    });

    expect(extractErrorMessage(response, 'fallback')).toBe('Email is required.');
  });

  it('falls back when the body has no errors object (e.g. a network error)', () => {
    const response = new HttpErrorResponse({ error: new ProgressEvent('error') });

    expect(extractErrorMessage(response, 'fallback')).toBe('fallback');
  });

  it('falls back when the body is a plain string', () => {
    const response = new HttpErrorResponse({ error: 'Internal Server Error' });

    expect(extractErrorMessage(response, 'fallback')).toBe('fallback');
  });

  it('falls back when errors is an empty object', () => {
    const response = new HttpErrorResponse({ error: { errors: {} } });

    expect(extractErrorMessage(response, 'fallback')).toBe('fallback');
  });

  it('falls back when there is no body at all', () => {
    const response = new HttpErrorResponse({});

    expect(extractErrorMessage(response, 'fallback')).toBe('fallback');
  });
});
