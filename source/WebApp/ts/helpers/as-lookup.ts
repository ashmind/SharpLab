import type { DeepReadonly } from './deep-readonly';

export default function asLookup<T>(object: T) {
    return object as DeepReadonly<
        string extends keyof T
            ? { [key: string]: T[keyof T]|undefined }
            : { [Key in keyof T]: T[Key]; } & { [key: string]: T[keyof T]|undefined }
    >;
}