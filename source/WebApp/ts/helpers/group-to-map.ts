export default function groupToMap<T, TKey>(array: ReadonlyArray<T>, getKey: (item: T) => TKey): Map<TKey, Array<T>> {
    const map = new Map<TKey, Array<T>>();
    for (const item of array) {
        const key = getKey(item);
        let group = map.get(key);
        if (!group) {
            group = [];
            map.set(key, group);
        }
        group.push(item);
    }
    return map;
}