import { Navigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import type { ReactNode } from "react";
import AsyncState from "../components/ui/AsyncState";

interface PrivateRouteProps {
  children: ReactNode;
}

export default function PrivateRoute({ children }: PrivateRouteProps) {
  const { isAuthenticated, isAuthLoading } = useAuth();

  if (isAuthLoading) {
    return (
      <main className="page-state-container">
        <AsyncState type="loading" message="Loading your session..." />
      </main>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
