import React from 'react';

interface OtherPlayersProps {
    players: string[];
}

const OtherPlayers: React.FC<OtherPlayersProps> = ({ players }) => {
    return (
        <>
            {players.length > 0 && (
                <div key="other-players" className="other-players">
                    <p>Other Players:</p>
                    <ul>
                        {players.map((playerName) => (
                            <li key={playerName}>{playerName}</li>
                        ))}
                    </ul>
                </div>
            )}
        </>
    );
};
export default OtherPlayers;
