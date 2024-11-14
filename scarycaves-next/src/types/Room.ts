import { Mob } from './Mob';

export interface Room {
    id: number;
    name: string;
    description: string;
    exits: string[]; // list of Directions
    playersInRoom: string[];
    zoneName: string;
    mobsInRoom: Mob[];
}
