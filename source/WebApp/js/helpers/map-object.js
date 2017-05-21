export default function mapObject(object, mapEntry) {
    const result = {};
    for (const key in object) {
        const [newKey, newValue] = mapEntry(key, object[key]);
        result[newKey] = newValue;
    }
    return result;
}