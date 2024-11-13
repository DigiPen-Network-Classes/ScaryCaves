"use client";
import { useEffect } from 'react';
const BootstrapClient : React.FC = () => {
    useEffect(() => {
        import('bootstrap/dist/js/bootstrap.bundle.min.js').catch(error =>
            console.error("Bootstrap failed to load:", error)
        );
    }, []);
    return null;
}
export default BootstrapClient;
