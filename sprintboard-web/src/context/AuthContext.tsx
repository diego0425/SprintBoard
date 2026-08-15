import { useEffect, useMemo, useState, type ReactNode } from "react";
import type { AuthResponse, JwtUser } from "../types/auth";
import { isTokenExpired } from "../utils/jwt";
import { getMe } from "../services/userService";
import { AuthContext } from "./authContextDefinition";

interface AuthProviderProps {
  children: ReactNode;
}

function mapCurrentUser(user: Awaited<ReturnType<typeof getMe>>): JwtUser {
  return {
    userId: user.id,
    fullName: user.fullName,
    username: user.username,
    email: user.email,
    profileImageUrl: user.profileImageUrl,
  };
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<JwtUser | null>(null);
  const [isAuthLoading, setIsAuthLoading] = useState(true);

  useEffect(() => {
    async function loadAuthenticatedUser() {
      const storedToken = localStorage.getItem("accessToken");

      if (!storedToken || isTokenExpired(storedToken)) {
        localStorage.removeItem("accessToken");
        setIsAuthLoading(false);
        return;
      }

      try {
        setToken(storedToken);
        const currentUser = await getMe();
        setUser(mapCurrentUser(currentUser));
      } catch (loadError) {
        console.error(loadError);
        localStorage.removeItem("accessToken");
        setToken(null);
        setUser(null);
      } finally {
        setIsAuthLoading(false);
      }
    }

    loadAuthenticatedUser();
  }, []);

  async function login(authResponse: AuthResponse) {
    localStorage.setItem("accessToken", authResponse.accessToken);
    setToken(authResponse.accessToken);

    try {
      const currentUser = await getMe();
      setUser(mapCurrentUser(currentUser));
    } catch (loginError) {
      localStorage.removeItem("accessToken");
      setToken(null);
      throw loginError;
    }
  }

  function logout() {
    localStorage.removeItem("accessToken");
    setToken(null);
    setUser(null);
  }

  const value = useMemo(
    () => ({
      token,
      user,
      isAuthenticated: Boolean(token),
      isAuthLoading,
      login,
      logout,
      setUser,
    }),
    [token, user, isAuthLoading]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
