"use client";
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import React, { useState } from 'react';
import { useGoogleReCaptcha } from "react-google-recaptcha-v3";
import GoogleCaptchaWrapper  from '@/components/GoogleCaptchaWrapper';

export default function LoginPage() {
    return (
        <GoogleCaptchaWrapper>
            <LoginPageContent/>
        </GoogleCaptchaWrapper>
    );
}

const LoginPageContent = () => {
    const [playerName, setPlayerName] = useState('');
    const [password, setPassword] = useState('');
    const router = useRouter();
    const { executeRecaptcha } = useGoogleReCaptcha();

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!executeRecaptcha) {
            alert('Recaptcha not ready');
            return;
        }
        executeRecaptcha("login").then(async (token) => {
            const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:8000";

            const response = await fetch(`${apiBaseUrl}/Home/Login`, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: JSON.stringify({playerName, password, token}),
            });
            if (response.ok) {
                router.push('/scarycave');
                return;
            } else {
                alert('Login failed');
            }
        });
    };
    return (
        <div>
            <h2 className="display-6">The Scary Cave Login Form</h2>
            <br/>

            <div className="bg-blue-100 border-t border-b border-blue-500 text-blue-700 px-4 py-3" role="alert">
                <p className="font-bold">Important!</p>
                <p className="text-sm">
                    Since this is Just For Fun, all accounts expire after one day so you may need to re-register.<br/>
                    Player Names are restricted to letters only and must be at least 4 characters long.
                </p>
                <p><Link href="/register" className={"underline"} passHref>Are you a New Player? Register here!</Link></p>
            </div>
            <br/>
            <form onSubmit={handleLogin}>
                <div className="formGroup">
                    <label>Player Name:</label>
                    <input type="text"
                           className="form-control"
                           value={playerName}
                           onChange={e => setPlayerName(e.target.value)}
                           required
                    />
                </div>
                <div className="formGroup">
                    <label>Password:</label>
                    <input type="password"
                           className="form-control"
                           value={password}
                           onChange={e => setPassword(e.target.value)}
                           required
                    />
                </div>
                <div className="formGroup">
                    <button type="submit" className="btn btn-primary mt-3">Login</button>
                </div>
            </form>
        </div>
    );
};
