import React from 'react';

interface OtherPlayersProps {
    players: string[];
    thisPlayer: string;
}

const OtherPlayers: React.FC<OtherPlayersProps> = ({ players, thisPlayer }) => {
    const otherPlayers = players.filter(playerName => playerName !== thisPlayer);
    return (
        <>
            {otherPlayers.length > 0 && (
                <div key="other-players" className="other-players">
                    <p>Other Players:</p>
                    <ul>
                        {otherPlayers.map((playerName) => (
                            <li key={playerName}>{playerName}</li>
                        ))}
                    </ul>
                </div>
            )}
        </>
    );
};
export default OtherPlayers;
