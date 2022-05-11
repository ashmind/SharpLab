export default function mapObject<TOldKey extends string, TOldValue, TNewKey extends string, TNewValue>(
    object: { [key in TOldKey]: TOldValue },
    mapEntry: (key: TOldKey, value: TOldValue) => readonly [TNewKey, TNewValue]
) {
    const result = {} as { [key in TNewKey]: TNewValue };
    for (const key in object) {
        const [newKey, newValue] = mapEntry(key, object[key]);
        result[newKey] = newValue;
    }
    return result;
}