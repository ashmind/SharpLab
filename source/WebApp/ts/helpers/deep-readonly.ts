export type DeepReadonly<T> = {
    readonly [P in keyof T]:
        T[P] extends string|number|boolean|undefined|null
            ? T[P]
            : T[P] extends RegExp
                ? RegExp // https://github.com/microsoft/TypeScript/issues/37751
                : T[P] extends Array<infer U>
                    ? ReadonlyArray<DeepReadonly<U>>
                    : DeepReadonly<T[P]>;
};