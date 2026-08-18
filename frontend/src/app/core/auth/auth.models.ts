export type Gender = 'Female' | 'Male' | 'Other' | 'PreferNotToSay';

export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
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
  firstName: string | null;
  phoneNumber: string | null;
  roles: string[];
}

// DisplayName isn't part of this — it's set once at registration, not editable from the profile.
export interface UpdateProfileRequest {
  userName: string;
  phoneNumber: string | null;
  firstName: string | null;
  lastName: string | null;
  gender: Gender | null;
  dateOfBirth: string | null; // yyyy-MM-dd, matches an <input type="date"> value
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  userName: string;
  displayName: string;
  phoneNumber: string | null;
  firstName: string | null;
  lastName: string | null;
  gender: Gender | null;
  dateOfBirth: string | null;
  roles: string[];
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
