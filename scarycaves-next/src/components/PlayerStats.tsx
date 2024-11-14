import React from 'react';
import { Player } from '../types/Player';

interface PlayerStatsProps {
    player: Player;
}

const PlayerStats : React.FC<PlayerStatsProps> = ({ player }) => {
    return (
        <div className="player-stats">
            <p>Your Name: {player.name}</p>
        </div>
    );
}

export default PlayerStats;
