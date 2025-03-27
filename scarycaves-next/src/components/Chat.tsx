'use client';
import { HubConnection } from '@microsoft/signalr';
import { useEffect, useState } from 'react';

type ChatProps = {
    connection: HubConnection | null;
    userName: string;
};

export default function Chat({ connection, userName }: ChatProps) {
    const [messages, setMessages] = useState<string[]>([]);
    const [input, setInput] = useState('');

    useEffect(() => {
        if (!connection) return;

        const onMessage = (message: string) => {
            const timestamp = new Date().toLocaleTimeString('en-GB');
            setMessages(prevMessages => [
                `${message} (${timestamp})`,
                ...prevMessages
            ].slice(0, 50));
        };

        connection.on('ReceiveMessage', onMessage);

        return () => {
            connection.off('ReceiveMessage', onMessage);
        };

    }, [connection]);

    const sendMessage = async () => {
        if (connection && input.trim()) {
            try {
                await connection.invoke("SendMessage", "global", input.trim());
                setInput('');
            } catch (err) {
                console.error("Failed to send message:", err);
            }
        }
    };

    return (
        <div className="p-2 border rounded bg-light">
            <h4>Global Chat</h4>
            <div className="chat-log overflow-auto" style={{maxHeight: 200}}>
                {messages.map((msg, i) => (
                    <div key={i} className="small">{msg}</div>
                ))}
            </div>
            <div className="d-flex mt-2">
                <input
                    type="text"
                    className="form-control me-2"
                    value={input}
                    onChange={e => setInput(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && sendMessage()}
                />
                <button className="btn btn-primary" onClick={sendMessage}>Send</button>
            </div>
        </div>
    );
}
