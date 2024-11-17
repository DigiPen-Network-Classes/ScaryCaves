"use client";
import {GoogleReCaptchaProvider} from "react-google-recaptcha-v3";
import React from 'react';

export default function GoogleCaptchaWrapper({ children, }: { children: React.ReactNode; }) {
    return (
        <GoogleReCaptchaProvider reCaptchaKey={process.env.NEXT_PUBLIC_RECAPTCHA_SITE_KEY || "NO_KEY"}
                                 scriptProps={{ 
                                     async: false,
                                     defer: false,
                                     appendTo: "head",
                                     nonce: undefined,
                                 }}
        >
            {children}
        </GoogleReCaptchaProvider>
    );
}
