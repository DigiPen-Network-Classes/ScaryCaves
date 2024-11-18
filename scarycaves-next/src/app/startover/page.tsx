"use client";
import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

const StartOverPage = () => {
    const router = useRouter();
    useEffect(() => {
        const startOver =async () => {
            const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:8000";
            const response = await fetch(`${apiBaseUrl}/Home/StartOver`, {
                method: 'POST',
                credentials: 'include'
            });
            if (response.ok) {
                router.push('/scarycave');
            } else {
                alert('StartOver Failed');
                router.push('/login');
            }
        };
        startOver();
    }, [router]);
    return <div>
        <h2 className="display-6">Starting Over! Resetting your player account.</h2>
    </div>;
};
export default StartOverPage;
