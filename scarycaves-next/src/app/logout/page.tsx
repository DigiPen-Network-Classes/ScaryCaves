"use client";
import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

const LogoutPage = () => {
    const router = useRouter();
    useEffect(() => {
            const logout  =async () => {
                const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:8000";
                const response = await fetch(`${apiBaseUrl}/Home/Logout`, {
                    method: 'POST',
                    credentials: 'include'
                });
                if (response.ok) {
                    router.push('/');
                } else {
                    alert('Logout failed?');
                }
            };
            logout();
        }, [router]);
    return <div>
            <h2 className="display-6">Logout</h2>
        </div>;
};
export default LogoutPage;
