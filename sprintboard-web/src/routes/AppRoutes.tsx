import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import PrivateRoute from "./PrivateRoute";
import PublicRoute from "./PublicRoute";
import LoginPage from "../pages/auth/LoginPage";
import RegisterPage from "../pages/auth/RegisterPage";
import BoardsPage from "../pages/boards/BoardsPage";
import BoardDetailsPage from "../pages/boards/BoardDetailsPage";
import InvitationsPage from "../pages/invitations/InvitationsPage";
import MePage from "../pages/profile/MePage";
import NotFoundPage from "../pages/NotFoundPage";

export default function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>

        <Route
          path="/"
          element={
            <PrivateRoute>
              <Navigate to="/boards" replace />
            </PrivateRoute>
          }
        />

        <Route
          path="/login"
          element={
            <PublicRoute>
              <LoginPage />
            </PublicRoute>
          }
        />

        <Route
          path="/register"
          element={
            <PublicRoute>
              <RegisterPage />
            </PublicRoute>
          }
        />

        <Route
          path="/me"
          element={
            <PrivateRoute>
              <MePage/>
            </PrivateRoute>
          }
        />

        <Route
          path="/boards"
          element={
            <PrivateRoute>
              <BoardsPage />
            </PrivateRoute>
          }
        />

        <Route
          path="/boards/:boardId"
          element={
            <PrivateRoute>
              <BoardDetailsPage />
            </PrivateRoute>
          }
        />

        <Route
          path="/invitations"
          element={
            <PrivateRoute>
              <InvitationsPage />
            </PrivateRoute>
          }
        />

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
}