import React from 'react';
import { Mob } from '../types/Mob';

interface MobListProps {
    mobs: Mob[];
}

const MobList: React.FC<MobListProps> = ({ mobs }) => {
    return (
        <div key="mob-list" className="mob-list">
            {mobs.map((m) => (
                <p key={m.instanceId} className="mob-description">{m.description}</p>
                ))}
        </div>
    );
}
export default MobList;
