// @ts-ignore
Object.fromEntries = Object.fromEntries || function(entries) {
    const object = {};
    for (const entry of entries) {
        object[entry[0]] = entry[1];
    }
    return object;
};