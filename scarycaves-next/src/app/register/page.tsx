"use client";
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

const RegisterPage = () => {
    const [playerName, setPlayerName] = useState('');
    const [password, setPassword] = useState('');
    const router = useRouter();

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        const response = await fetch('http://localhost:8000/Home/Register', {
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
            alert('Registration failed - try a different name.');
        }
    };
    return (
        <div>
            <h2 className="display-6">Register</h2>
            <p><Link href="/login" passHref>Existing Player? Login Here</Link></p>
            <br/>
            <form onSubmit={handleRegister}>
                <div className="formGroup">
                    <label>Player Name:</label>
                    <input type="text" className="form-control" value={playerName} onChange={e => setPlayerName(e.target.value)}/>
                </div>
                <div className="formGroup">
                    <label>Password:</label>
                    <input type="password"
                           className="form-control"
                           value={password}
                           onChange={e => setPassword(e.target.value)}
                    />
                </div>
                <button type="submit" className="btn btn-primary">Create Player</button>
            </form>
        </div>
    );
};
export default RegisterPage;
