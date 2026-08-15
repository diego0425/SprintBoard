import { createContext } from "react";
import type { AuthResponse, JwtUser } from "../types/auth";

export interface AuthContextData {
  token: string | null;
  user: JwtUser | null;
  isAuthenticated: boolean;
  isAuthLoading: boolean;
  login: (authResponse: AuthResponse) => Promise<void>;
  logout: () => void;
  setUser: React.Dispatch<React.SetStateAction<JwtUser | null>>;
}

export const AuthContext = createContext<AuthContextData | undefined>(undefined);
