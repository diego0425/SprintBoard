import { Link } from "react-router-dom";
import RegisterForm from "../../components/auth/RegisterForm";

export default function RegisterPage() {
  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>SprintBoard</h1>
        <p className="auth-subtitle">Create your account</p>

        <RegisterForm />

        <p className="auth-footer">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </div>
    </div>
  );
}