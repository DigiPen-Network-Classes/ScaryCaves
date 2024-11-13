"use client";
import { useRouter } from 'next/navigation';
import { useEffect, useState, useRef } from 'react';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { RoomState } from '../../types/RoomState';
import MobList  from '../../components/MobList';
import OtherPlayers from '../../components/OtherPlayers';
import RoomExits  from '../../components/RoomExits';
import PlayerStats from '../../components/PlayerStats';

const RoomPage : React.FC = () => {
    const router = useRouter();
    const [roomState, setRoomState] = useState<RoomState | null>(null);
    // useRef to only create this once
    const connectionRef = useRef<HubConnection | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [messages, setMessages] = useState<string[]>([]);

    useEffect(() => {
        // initialize SignalR connection, if authorized
        if (!connectionRef.current) {
            // only once
            const connection = new HubConnectionBuilder()
                .withUrl("http://localhost:8000/GameHub", {withCredentials: true})
                .withAutomaticReconnect()
                .build();
            connectionRef.current = connection;

            const printMessage = (message: string) => {
                setMessages(prevMessages => {
                    const timestamp = new Date().toLocaleTimeString("en-GB");
                    const completeMessage = `${message} (${timestamp})`;
                    return [completeMessage, ...prevMessages].slice(0, 10);
                });
            };

            const startConnection = async () => {
                // see if we are already authenticated - if not redirect without connection attempt
                try {
                    console.log("Check Status to see if we are logged in");
                    const statusResponse = await fetch('http://localhost:8000/Home/Status', {credentials: 'include'});
                    if (!statusResponse.ok) {
                        console.log("Not logged in, redirecting to login");
                        router.push('/login');
                        return;
                    }

                    connection.onclose((error) => {
                        console.log("connection.onclose: ", error);
                        router.push('/login');
                    });

                    connection.on("UpdateRoomState", (newRoomState: RoomState) => {
                        console.log("UpdateRoomState received: ", newRoomState);
                        setRoomState(newRoomState); // update when data is received
                        setError(null); // clear error
                        setMessages([]); // clear messages
                    });

                    connection.on("PlayerEntered", (playerName: string) => {
                        console.log("PlayerEntered received: ", playerName);
                        printMessage(`${playerName} has entered the room.`);
                        setRoomState(prevState => {
                            if (!prevState) return null;
                            return {
                                ...prevState,
                                room: {
                                    ...prevState.room,
                                    playersInRoom: [...prevState.room.playersInRoom, playerName],
                                }
                            };
                        });
                    });

                    connection.on("PlayerLeft", (playerName: string) => {
                        console.log("PlayerLeft received: ", playerName);
                        printMessage(`${playerName} has left the room.`);

                        setRoomState(prevState => {
                            if (!prevState) return null;
                            return {
                                ...prevState,
                                room: {
                                    ...prevState.room,
                                    playersInRoom: prevState.room.playersInRoom.filter(p => p !== playerName),
                                }
                            };
                        });
                    });

                    connection.on("ReceiveMessage", (message: string) => {
                        console.log("Message: ", message);
                    });

                    await connection.start();
                } catch (error) {
                    console.error(`Connection failure: ${error}`);
                    router.push('/login');
                }
            };
            startConnection();
        }

        return () => {
            // cleanup
            connectionRef.current?.stop().then(() => console.log("Connection stopped"));
        };
    }, [router]);

    if (!roomState) {
        return <div>Loading...</div>;
    }
    return (
        <div className="text-center">
            <h1 className="room-name display-4">Room {roomState.room.id}: {roomState.room.name}</h1>
            <p className="room-description">{roomState.room.description}</p>

            {error && <div className="alert alert-danger">{error}</div>}

            <MobList mobs={roomState.room.mobsInRoom} />

            <OtherPlayers players={roomState.room.playersInRoom} thisPlayer={roomState.player.name} />

            <p className="player-action">Some things you might do:</p>
            <RoomExits roomState={roomState} connection={connectionRef.current} />

            <div className="messages">
                {messages.map((msg, index) => (
                    <div key={index} className="alert alert-info">{msg}</div>
                ))}
            </div>

            <PlayerStats playerState={roomState.player}/>
        </div>
    );
};

export default RoomPage;
