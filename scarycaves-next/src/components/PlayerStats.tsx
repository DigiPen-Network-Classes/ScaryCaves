import React from 'react';
import { Player } from '../types/Player';

interface PlayerStatsProps {
    playerState: Player;
}

const PlayerStats : React.FC<PlayerStatsProps> = ({ playerState }) => {
    return (
        <div className="player-stats">
            <p>Your Name: {playerState.name}</p>
        </div>
    );
}

export default PlayerStats;
