"use client";
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { RoomState } from '../../types/RoomState';
import MobList  from '../../components/MobList';
import OtherPlayers from '../../components/OtherPlayers';
import RoomExits  from '../../components/RoomExits';
import PlayerStats from '../../components/PlayerStats';

const RoomView : React.FC = () => {
    const router = useRouter();
    const [roomState, setRoomState] = useState<RoomState | null>(null);
    const [connection, setConnection] = useState<HubConnection | null>(null);

    useEffect(() => {
        // initialize SignalR connection
        const connectSignalR = async () => {
            const connection = new HubConnectionBuilder()
                .withUrl("http://localhost:8000/gameHub", {
                    withCredentials: true, // cookies in request
                })
                .withAutomaticReconnect()
                .build();

            connection.onclose((error) => {
                router.push('/login');
            });

            connection.on("UpdateRoomState", (newRoomState: RoomState) => {
                setRoomState(newRoomState); // update when data is received
            });

            connection.on("ReceiveMessage", (message: string) => {
                console.log("ReceiveMessage: ", message);
            });

            try {
                await connection.start();
                setConnection(connection);
            } catch (error) {
                router.push('/login');
            }
        };

        connectSignalR();

    return () => {
            if (connection) {
                connection.stop();
            }
        };
    }, []);

    if (!roomState) {
        return <div>Loading...</div>;
    }
    return (
        <div className="text-center">
            <h1 className="room-name display-4">Room {roomState.room.id}: {roomState.room.name}</h1>
            <p className="room-description">{roomState.room.description}</p>

            <MobList mobs={roomState.room.mobsInRoom} />
            <OtherPlayers players={roomState.room.playersInRoom} />

            <p className="player-action">Some things you might do:</p>
            <RoomExits roomState={roomState} connection={connection} />

            <PlayerStats playerState={roomState.player}/>
        </div>
    );
};

export default RoomView;
