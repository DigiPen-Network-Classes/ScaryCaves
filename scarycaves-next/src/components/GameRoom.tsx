"use client";
import { useEffect, useState } from 'react';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

// Define the shape of messages state
interface Message {
    text: string;
    id: number;
}

const GameRoom: React.FC = () => {
    const [messages, setMessages] = useState<Message[]>([]);
    const [connection, setConnection] = useState<HubConnection | null>(null);

    useEffect(() => {
        const connectSignalR = async () => {
            const connection = new HubConnectionBuilder()
                .withUrl("http://localhost:8000/gameHub", {
                withCredentials: true})
                .withAutomaticReconnect()
                .build();

            connection.on("ReceiveMessage", (message: string) => {
                setMessages(prevMessages => [
                    ...prevMessages,
                    { text: message, id: prevMessages.length + 1 }
                ]);
            });

            await connection.start();
            setConnection(connection);
        };

        connectSignalR();

        // Clean up the connection when component unmounts
        return () => {
            if (connection) {
                connection.stop();
            }
        };
    }, []);

    return (
        <div>
            <h1>Game Room</h1>
            <ul>
                {messages.map((msg) => (
                    <li key={msg.id}>{msg.text}</li>
                ))}
            </ul>
        </div>
    );
};

export default GameRoom;
