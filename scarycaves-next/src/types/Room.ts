import { MobState } from './MobState';

export interface Room {
    id: number;
    name: string;
    description: string;
    exits: Record<string, number>; // using string keys to represent Direction
    playersInRoom: string[]; // list of players in the room
    zoneName: string;
    mobsInRoom: MobState[]; // array of mobs in the room (TODO defined later)
}
