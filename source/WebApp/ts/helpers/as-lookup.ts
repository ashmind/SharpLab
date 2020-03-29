import type { DeepReadonly } from './deep-readonly';

export default function asLookup<T>(object: T) {
    return object as DeepReadonly<{
        [Key in keyof T]: T[Key];
    } & { [key: string]: T[keyof T]|undefined }>;
}