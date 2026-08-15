import { Link } from "react-router-dom";
import LoginForm from "../../components/auth/LoginForm";

export default function LoginPage() {
  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>SprintBoard</h1>
        <p className="auth-subtitle">Sign in to continue</p>

        <LoginForm />

        <p className="auth-footer">
          Don&apos;t have an account? <Link to="/register">Create one</Link>
        </p>
      </div>
    </div>
  );
}