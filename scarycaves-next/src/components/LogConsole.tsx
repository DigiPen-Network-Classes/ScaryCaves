// components/LogConsole.tsx
"use client";

import React from 'react';

interface LogConsoleProps {
    logs: string[];
}

const LogConsole: React.FC<LogConsoleProps> = ({ logs }) => {
    return (
        <div className="log-console">
            <h2>Room Events</h2>
            <ul>
                {logs.map((log, index) => (
                    <li key={index}>{log}</li>
                ))}
            </ul>
        </div>
    );
};

export default LogConsole;
