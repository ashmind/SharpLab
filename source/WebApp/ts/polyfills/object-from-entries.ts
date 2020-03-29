Object.fromEntries = Object.fromEntries || (function<TKey extends string|number, TValue>(entries: Iterable<readonly [TKey, TValue]>) {
    const object = {} as { [key in TKey]: TValue };
    for (const entry of entries) {
        object[entry[0]] = entry[1];
    }
    return object;
} as typeof Object.fromEntries);