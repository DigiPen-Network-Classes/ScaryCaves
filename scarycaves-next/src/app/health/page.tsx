"use client";
import { useEffect, useState } from 'react';

interface HealthStatus {
    apiHealth: boolean;
    redisHealth: boolean;
    orleansHealth: boolean;
    apiBaseUrl: string;
    nextBaseUrl: string;
    recaptchaSiteKeyLength: number;
    error?: string;
}
export default function HealthPage() {
   const [healthStatus, setHealthStatus] = useState<HealthStatus|null>(null);

    useEffect(() => {
        const checkHealth = async() => {
            const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://127.0.0.1:8000";
            const nextBaseUrl = process.env.NEXT_PUBLIC_BASE_URL || "http://127.0.0.1:3000";
            const recaptchaLength = process.env.NEXT_PUBLIC_RECAPTCHA_SITE_KEY?.length || 0;
            try {
                const response = await fetch(`${apiBaseUrl}/Home/Health`);
                const apiHealthStatus = await response.json();
                setHealthStatus({
                    apiHealth: response.ok,
                    redisHealth: apiHealthStatus.redis,
                    orleansHealth: apiHealthStatus.orleans,
                    apiBaseUrl: apiBaseUrl,
                    nextBaseUrl: nextBaseUrl,
                    recaptchaSiteKeyLength: recaptchaLength,
                });
            } catch (error) {
                const errorMessage = (error as Error).message || "Unknown Error";
                setHealthStatus({
                    apiHealth: false,
                    redisHealth: false,
                    orleansHealth: false,
                    apiBaseUrl: apiBaseUrl,
                    nextBaseUrl: nextBaseUrl,
                    recaptchaSiteKeyLength: recaptchaLength,
                    error: errorMessage,
                });
            }
        };
        checkHealth();
    }, []);
    return (
        <div>
            <h1>System Health</h1>
            {healthStatus ? (
                <div>
                    <h2>API Health: {healthStatus.apiHealth ? "Good" : "Bad"}</h2>
                    <h2>Redis Health: {healthStatus.redisHealth ? "Good" : "Bad"}</h2>
                    <h2>Orleans Health: {healthStatus.orleansHealth ? "Good" : "Bad"}</h2>
                    <h2>API Base Url: {healthStatus.apiBaseUrl}</h2>
                    <h2>Next Base Url: {healthStatus.nextBaseUrl}</h2>
                    <h2>Recaptcha Site Key: {healthStatus.recaptchaSiteKeyLength > 0 ? "Good" : "Bad"}</h2>
                    {healthStatus.error && <p>Error: {healthStatus.error}</p>}
                </div>
            ) : (
                <p>Loading...</p>
            )}
        </div>
    )
}
