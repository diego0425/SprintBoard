import { api } from "./api";
import type { AuthResponse, LoginRequest, RegisterRequest } from "../types/auth";

export async function loginUser(request: LoginRequest): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>("/auth/login", request);
  return response.data;
}

export async function registerUser(request: RegisterRequest): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>("/auth/register", request);
  return response.data;
}
