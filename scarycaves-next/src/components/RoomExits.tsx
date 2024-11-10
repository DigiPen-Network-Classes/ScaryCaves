import React from 'react';
import { RoomState } from '../types/RoomState';
import { HubConnection } from '@microsoft/signalr';
import {Dir} from "node:fs";

interface RoomExitsProps {
    roomState: RoomState;
    connection: HubConnection | null;
}

const RoomExits: React.FC<RoomExitsProps> = ({ roomState, connection }) => {
    const handleExitClick = (direction: string) => {
        if (connection) {
            connection.invoke("MoveTo", direction)
                .catch(err => console.error("Failed to move", err));
        }
    };

    return (
        <ul className="player-action">
            {Object.entries(roomState.room.exits).map(([direction, roomId]) => (
                <li key={direction} className="player-action">
                    <button
                        type="button"
                        className="btn btn-outline-primary player-action"
                        onClick={() => handleExitClick(direction)}
                    >
                        GO {direction}
                </button>
                </li>
            ))}
        </ul>
    );
}
export default RoomExits;
