export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  username: string;
  email: string;
  password: string;
  repeatPassword: string;
}

export interface JwtUser {
  userId: string;
  fullName: string;
  email: string;
  username: string;
  profileImageUrl?: string | null;
}
