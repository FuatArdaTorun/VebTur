import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CurrentUser, LoginRequest, LoginResponse, RegisterRequest } from './auth.models';

// Shared by both admin and customer sessions — this service authenticates any signed-in user,
// not just admins (see AuthController.Register in the backend for the "Customer" role path).
const TOKEN_KEY = 'vebtur_token';
const USER_KEY = 'vebtur_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1`;

  readonly currentUser = signal<CurrentUser | null>(readStoredUser());
  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  login(request: LoginRequest) {
    return this.http.post<LoginResponse>(`${this.baseUrl}/auth/login`, request).pipe(tap((response) => this.storeSession(response)));
  }

  register(request: RegisterRequest) {
    return this.http.post<LoginResponse>(`${this.baseUrl}/auth/register`, request).pipe(tap((response) => this.storeSession(response)));
  }

  private storeSession(response: LoginResponse): void {
    const user: CurrentUser = { email: response.email, displayName: response.displayName, roles: response.roles };
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  hasRole(role: string): boolean {
    return this.currentUser()?.roles.includes(role) ?? false;
  }
}

function readStoredUser(): CurrentUser | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as CurrentUser;
  } catch {
    return null;
  }
}
