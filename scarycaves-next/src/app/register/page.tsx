"use client";
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import React, { useState } from 'react';
import { useGoogleReCaptcha } from "react-google-recaptcha-v3";
import GoogleCaptchaWrapper  from '@/components/GoogleCaptchaWrapper';

export default function RegisterPage()
{
    return (
        <GoogleCaptchaWrapper>
            <RegisterPageContent/>
        </GoogleCaptchaWrapper>
    );
}

const RegisterPageContent = () => {
    const [playerName, setPlayerName] = useState('');
    const [password, setPassword] = useState('');
    const router = useRouter();
    const { executeRecaptcha } = useGoogleReCaptcha();

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!executeRecaptcha) {
            alert('Recaptcha not ready');
            return;
        }
        executeRecaptcha("register").then(async (token) => {

            const response = await fetch('http://localhost:8000/Home/Register', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: JSON.stringify({playerName, password, token})
            });
            if (response.ok) {
                router.push('/scarycave');
            } else {
                alert('Registration failed - try a different name.');
            }
        });
    };
    return (
        <div>
            <h2 className="display-6">The Scary Cave Registration Form</h2>
            <br/>

            <div className="bg-blue-100 border-t border-b border-blue-500 text-blue-700 px-4 py-3" role="alert">
                <p className="font-bold">Important!</p>
                <p className="text-sm">
                    Since this is Just For Fun, all accounts expire after one day so you may need to re-register.<br/>
                    Player Names are restricted to letters only and must be at least 4 characters long.
                </p>
                <p><Link href="/login" className={"underline"} passHref>Are you an Existing Player? Login Here</Link></p>
            </div>
                <br/>
                <form onSubmit={handleRegister}>
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
                        <button type="submit" className="btn btn-primary mt-3">Create Player</button>
                    </div>
                </form>
            </div>
            );
            };
