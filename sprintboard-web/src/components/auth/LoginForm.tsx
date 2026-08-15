import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { loginUser } from "../../services/authService";
import { useAuth } from "../../hooks/useAuth";

export default function LoginForm() {
  const navigate = useNavigate();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    try {
      setError("");
      setIsSubmitting(true);

      const authResponse = await loginUser({ email, password });
      await login(authResponse);
      navigate("/boards");
    } catch (submitError) {
      console.error(submitError);
      setError("Invalid email or password.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="auth-form">
      <h2>Login</h2>

      <label>
        Email
        <input
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          placeholder="brucewayne@gmail.com"
          required
        />
      </label>

      <label>
        Password
        <input
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          placeholder="••••••••"
          required
        />
      </label>

      {error && <p className="auth-error">{error}</p>}

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Signing in..." : "Sign in"}
      </button>
    </form>
  );
}
