/* eslint-disable @typescript-eslint/no-explicit-any */
export type DeepReadonly<T> = {
    readonly [P in keyof T]: DeepReadonlyValue<T[P]>;
};

type DeepReadonlyValue<T> =
    T extends string|number|boolean|undefined|null ? T :
    T extends Function ? T :
    T extends RegExp ? T :
    T extends Array<infer U> ? ReadonlyArray<DeepReadonlyValue<U>> :
    T extends Promise<infer U> ? Promise<DeepReadonlyValue<U>> :
    DeepReadonly<T>;