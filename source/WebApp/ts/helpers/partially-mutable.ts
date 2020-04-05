export type PartiallyMutable<T, TMutableKeys extends keyof T> = {
    [TKey in Exclude<keyof T, TMutableKeys>]: T[TKey]
} & {
    -readonly[TKey in TMutableKeys]: NonReadonly<T[TKey]>
}

type NonReadonly<T> = T extends ReadonlyArray<infer U> ? Array<U> : T;