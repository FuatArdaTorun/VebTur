export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  email: string;
  displayName: string;
  roles: string[];
}

export interface CurrentUser {
  email: string;
  displayName: string;
  roles: string[];
}
