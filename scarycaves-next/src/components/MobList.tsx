import React from 'react';
import { MobState } from '../types/MobState';

interface MobListProps {
    mobs: MobState[];
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
