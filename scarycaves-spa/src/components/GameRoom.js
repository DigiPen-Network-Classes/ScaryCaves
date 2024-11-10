import React, { useEffect, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';

const GameRoom = () => {
    const [messages, setMessages] = useState([]);
    const [connection, setConnection] = useState(null);

    useEffect(() => {
        //eslint-disable-next-line react-hooks/exhaustive-deps
        const newConnection = new HubConnectionBuilder()
            .withUrl("http://localhost:8000/GameHub")  // URL of SignalR hub
            .withAutomaticReconnect()
            .build();

        newConnection.on("ReceiveMessage", message => {
            setMessages(prevMessages => [...prevMessages, message]);
        });
        newConnection.on("PlayerJoined", playerName => {
            setMessages(prevMessages => [...prevMessages, `${playerName} has joined the game`]);
        });

        newConnection.start().then(() => setConnection(newConnection))
            .catch(err => console.error("SignalR Connection Error: ", err));
        setConnection(connection);

        return () => {
            newConnection.stop();
        };
    }, []);

    return (
        <div>
            <h1>Game Room</h1>
            <ul>
                {messages.map((msg, index) => (
                    <li key={index}>{msg}</li>
                ))}
            </ul>
        </div>
    );
};

export default GameRoom;
