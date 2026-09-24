import {useState } from "react";
import type { FormEvent } from "react";
import { login,type AuthenticatedUser } from "./auth";

interface LoginPageProps{
    onLogin:(user:AuthenticatedUser) => void;
}

export default function LoginPage({
    onLogin,
}:LoginPageProps){
    const [email,setEmail] = useState("dispatcher@fieldops.com",);
    const [password, setPassword] = useState("FieldOps123!");
    const [error, setError] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);

    async function handleSubmit(event:FormEvent<HTMLFormElement>,) {
        event.preventDefault();
        setError("");
        setIsSubmitting(true);

        try{
            const response = await login({email,password});
            onLogin(response.user);
        }catch(exception){
            setError(exception instanceof Error ? exception.message : "Login failed.",);
        }finally{
            setIsSubmitting(false);
        }   
    }

    return (
        <main className="login-page">
            <section className="login-card">
                <p className="eyebrow">Operations Platform</p>
                <h1>FieldOps</h1>
                <p className="login-description">Sign in to manage work orders and technicians</p>

                <form onSubmit={handleSubmit}>
                    <label htmlFor="email">Email</label>
                    <input id="email" type="email" value={email} onChange={(event)=> setEmail(event.target.value)} autoComplete="email" required></input>

                    <label htmlFor="password">Password</label>
                    <input id="password" type="password" value={password} onChange={(event)=> setPassword(event.target.value)} autoComplete="current-password" required></input>

                    {error && (<div className="login-error" role="alert">{error}</div>)}

                    <button type="submit" disabled={isSubmitting}>{isSubmitting? "Signing in..." : "Sign in"}</button>
                </form>
            </section>
        </main>
    );
}


