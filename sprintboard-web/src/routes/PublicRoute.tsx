import { Navigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import type { ReactNode } from "react";

interface PublicRouteProps {
  children: ReactNode;
}

export default function PublicRoute({ children }: PublicRouteProps) {
  const { isAuthenticated, isAuthLoading } = useAuth();

  if (isAuthLoading) {
    return <div style={{ padding: "2rem" }}>Loading...</div>;
  }

  if (isAuthenticated) {
    return <Navigate to="/boards" replace />;
  }

  return <>{children}</>;
}