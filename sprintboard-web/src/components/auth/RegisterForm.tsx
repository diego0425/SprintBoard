import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { registerUser } from "../../services/authService";
import { useAuth } from "../../hooks/useAuth";

export default function RegisterForm() {
  const navigate = useNavigate();
  const { login } = useAuth();

  const [fullName, setFullName] = useState("");
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [repeatPassword, setRepeatPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    if (password !== repeatPassword) {
      setError("Passwords do not match.");
      return;
    }

    try {
      setIsSubmitting(true);

      const authResponse = await registerUser({
        fullName,
        username,
        email,
        password,
        repeatPassword,
      });

      await login(authResponse);
      navigate("/boards");
    } catch (submitError) {
      console.error(submitError);
      setError("Unable to create account.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="auth-form">
      <h2>Create account</h2>

      <label>
        Full name
        <input
          type="text"
          value={fullName}
          onChange={(event) => setFullName(event.target.value)}
          placeholder="Bruce Wayne"
          required
        />
      </label>

      <label>
        Username
        <input
          type="text"
          value={username}
          onChange={(event) => setUsername(event.target.value)}
          placeholder="Batman"
          required
        />
      </label>

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

      <label>
        Repeat password
        <input
          type="password"
          value={repeatPassword}
          onChange={(event) => setRepeatPassword(event.target.value)}
          placeholder="••••••••"
          required
        />
      </label>

      {error && <p className="auth-error">{error}</p>}

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Creating..." : "Create account"}
      </button>
    </form>
  );
}
