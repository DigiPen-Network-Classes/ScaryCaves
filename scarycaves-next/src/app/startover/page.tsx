"use client";
import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

const StartOverPage = () => {
    const router = useRouter();
    useEffect(() => {
        const startOver =async () => {
            const response = await fetch('http://localhost:8000/Home/StartOver', {
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
