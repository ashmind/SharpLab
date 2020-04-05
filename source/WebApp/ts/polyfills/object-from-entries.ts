/* eslint-disable @typescript-eslint/no-unnecessary-condition */
Object.fromEntries = Object.fromEntries ?? (<TValue, TKey extends PropertyKey>(entries: Iterable<readonly [TKey, TValue]>) => {
    const object = {} as { [key in TKey]: TValue };
    for (const entry of entries) {
        object[entry[0]] = entry[1];
    }
    return object;
});