"use client";
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

const LoginPage = () => {
    const [playerName, setPlayerName] = useState('');
    const [password, usePassword] = useState('');
    const router = useRouter();

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        const response = await fetch('http://localhost:8000/Home/Login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include', // include cookies in request
            body: JSON.stringify({playerName, password})
        });
        if (response.ok) {
            router.push('/scarycave');
        } else {
            alert('Login failed');
        }
    };
    return (
        <div>
            <h2 className="display-6">Login</h2>
            <p><Link href="/register" passHref>New Player? Register here</Link></p>
            <br/>
            <form onSubmit={handleLogin}>
                <div className="formGroup">
                    <label>Player Name:</label>
                    <input type="text" className="form-control" value={playerName} onChange={e => setPlayerName(e.target.value)}/>
                </div>
                <div className="formGroup">
                    <label>Password:</label>
                    <input type="password" className="form-control" value={password} onChange={e => usePassword(e.target.value)}/>
                </div>
                <button type="submit" className="btn btn-primary">Login</button>
            </form>
        </div>
    );
};
export default LoginPage;
