"use client";
import { useRouter } from 'next/navigation';
import { useEffect, useState, useRef } from 'react';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { ClientPlayerView } from '../../types/ClientPlayerView';
import MobList  from '../../components/MobList';
import OtherPlayers from '../../components/OtherPlayers';
import RoomExits  from '../../components/RoomExits';
import PlayerStats from '../../components/PlayerStats';
import PlayerMessages from '../../components/PlayerMessages';
import { Mob } from '../../types/Mob';
import {Room} from '../../types/Room';
import Chat from '../../components/Chat';

const RoomPage : React.FC = () => {
    const router = useRouter();
    const [playerView, setPlayerView] = useState<ClientPlayerView | null>(
    {
        room: {
            id: 0,
            name: "",
            description: "",
            exits: [],
            playersInRoom: [],
            zoneName: "",
            mobsInRoom: [],
        },
        player: {
            name: "",
            currentRoomId: 0,
            currentZoneName: "",
            ownerAccountId: ""
        }
    });
    // useRef to only create this once
    const connectionRef = useRef<HubConnection | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [messages, setMessages] = useState<string[]>([]);

    useEffect(() => {
        const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:8000";
        // initialize SignalR connection, if authorized
        if (!connectionRef.current) {
            // only once
            const connection = new HubConnectionBuilder()
                .withUrl(`${apiBaseUrl}/GameHub`, {withCredentials: true})
                .withAutomaticReconnect()
                .build();
            connectionRef.current = connection;

            const printMessage = (message: string) => {
                setMessages(prevMessages => {
                    const timestamp = new Date().toLocaleTimeString("en-GB"); // this locale uses 24 hour time
                    const completeMessage = `${message} (${timestamp})`;
                    return [completeMessage, ...prevMessages].slice(0, 10); // only display most recent 10
                });
            };
            const startConnection = async () => {
                // see if we are already authenticated - if not redirect without connection attempt
                const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:8000";
                try {
                    console.log("Check Status to see if we are logged in");
                    const statusResponse = await fetch(`${apiBaseUrl}/Home/Status`, {credentials: 'include'});
                    if (!statusResponse.ok) {
                        console.log("Not logged in, redirecting to login");
                        router.push('/login');
                        return;
                    }

                    connection.onclose((error) => {
                        console.log("connection.onclose: ", error);
                        router.push('/login');
                    });

                    connection.on("UpdatePlayerView", (newPlayerView: ClientPlayerView) => {
                        console.log("UpdatePlayerView received: ", newPlayerView);
                        setPlayerView(newPlayerView);
                        setError(null); // clear error
                        setMessages([]); // clear messages
                    });

                    connection.on("PlayerEntered", (playerName: string) => {
                        console.log("PlayerEntered received: ", playerName);
                        printMessage(`${playerName} has entered the room.`);
                        setPlayerView(prevState => {
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
                        setPlayerView(prevState => {
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

                    connection.on("MobEntered", (mob: Mob) => {
                        console.log("MobEntered received: ", mob);
                        printMessage(`${mob.name} has entered the room.`);
                        setPlayerView(prevPlayerView => {
                            const updatedMobs = [...(prevPlayerView?.room.mobsInRoom || []), mob];
                            return {
                                ...prevPlayerView as ClientPlayerView,
                                room: {
                                    ...prevPlayerView?.room as Room,
                                    mobsInRoom: updatedMobs,
                                }
                            }
                        });
                    });

                    connection.on("MobLeft", (mob: Mob) => {
                        console.log("MobLeft received: ", mob);
                        printMessage(`${mob.name} has left the room.`);
                        setPlayerView(prevState => {
                            if (!prevState) return null;
                            return {
                                ...prevState,
                                room: {
                                    ...prevState.room,
                                    mobsInRoom: prevState.room.mobsInRoom.filter(m => m.instanceId !== mob.instanceId),
                                }
                            }
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

    const handleExitClick = (direction: string) => {
        if (connectionRef.current) {
            connectionRef.current
                .invoke("MoveTo", direction)
                .then(() => console.log("MoveTo", direction))
                .catch(err => console.error("Failed to move", err));
        }
    };

    if (!playerView) {
        return <div>Loading...</div>;
    }
    return (
        <div className="row">
            <div className="text-center col-md-8">
                <h1 className="room-name display-4">Room {playerView.room.id}: {playerView.room.name}</h1>
                <p className="room-description">{playerView.room.description}</p>

                {error && <div className="alert alert-danger">{error}</div>}

                <MobList mobs={playerView.room.mobsInRoom} />

                <OtherPlayers players={playerView.room.playersInRoom} thisPlayer={playerView.player.name} />

                <p className="player-action">Some things you might do:</p>
                <RoomExits playerView={playerView} handleExitClick={handleExitClick} />

                <PlayerStats player={playerView.player}/>
            </div>
            <div className="col-md-4">
                <PlayerMessages messages={messages} />
            </div>
            <div className="text-center col-md-8">
                <Chat connection={connectionRef.current} userName={playerView.player.name} />
            </div>
        </div>
    );
};

export default RoomPage;
