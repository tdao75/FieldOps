import { useState, type FormEvent } from "react";
import { login, register, type AuthenticatedUser } from "./auth";

interface LoginPageProps {
  onLogin: (user: AuthenticatedUser) => void;
}

export default function LoginPage({ onLogin }: LoginPageProps) {
  const [isRegistering, setIsRegistering] = useState(false);

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("dispatcher@fieldops.com");
  const [password, setPassword] = useState("FieldOps123!");

  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);

    try {
      const response = isRegistering
        ? await register({
            firstName,
            lastName,
            email,
            password,
          })
        : await login({
            email,
            password,
          });

      onLogin(response.user);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Authentication failed.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  function switchMode() {
    setIsRegistering((current) => !current);
    setError("");
    setFirstName("");
    setLastName("");
    setEmail("");
    setPassword("");
  }

  return (
    <main className="login-page">
      <section className="login-card">
        <p className="eyebrow">OPERATIONS PLATFORM</p>
        <h1>FieldOps</h1>

        <p className="login-description">
          {isRegistering
            ? "Create a field technician account."
            : "Sign in to manage field operations."}
        </p>

        <form onSubmit={handleSubmit}>
          {isRegistering && (
            <>
              <label htmlFor="firstName">First name</label>
              <input
                id="firstName"
                value={firstName}
                onChange={(event) => setFirstName(event.target.value)}
                autoComplete="given-name"
                required
              />

              <label htmlFor="lastName">Last name</label>
              <input
                id="lastName"
                value={lastName}
                onChange={(event) => setLastName(event.target.value)}
                autoComplete="family-name"
                required
              />
            </>
          )}

          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            autoComplete="email"
            required
          />

          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete={isRegistering ? "new-password" : "current-password"}
            minLength={8}
            required
          />

          {error && (
            <div className="login-error" role="alert">
              {error}
            </div>
          )}

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting
              ? "Please wait..."
              : isRegistering
                ? "Create account"
                : "Sign in"}
          </button>

          <button
            type="button"
            className="auth-switch"
            onClick={switchMode}
            disabled={isSubmitting}
          >
            {isRegistering
              ? "Already have an account? Sign in"
              : "Need a technician account? Register"}
          </button>
        </form>
      </section>
    </main>
  );
}
