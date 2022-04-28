export type MutableContextValue<TValue, TSetValue = Exclude<TValue, undefined>> = readonly [
    value: TValue,
    setValue: (value: TSetValue) => void
];