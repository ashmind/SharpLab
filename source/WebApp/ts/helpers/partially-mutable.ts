export type PartiallyMutable<T, TMutableKeys extends keyof T> = Omit<T, TMutableKeys> & {
    -readonly[TKey in TMutableKeys]: NonReadonly<T[TKey]>
}

type NonReadonly<T> = T extends ReadonlyArray<infer U> ? Array<U> : T;