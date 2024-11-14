import React from 'react';

interface RoomExitsProps {
    playerView: {
        room: {
            exits: string[];
        };
    };
    handleExitClick: (direction:string) => void;
}

const RoomExits: React.FC<RoomExitsProps> = ({ playerView, handleExitClick }) => {
    return (
        <ul className="player-action">
            {playerView.room.exits.map((direction)=> (
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
