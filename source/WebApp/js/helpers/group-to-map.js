export default function groupToMap(array, getKey) {
    const map = new Map();
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