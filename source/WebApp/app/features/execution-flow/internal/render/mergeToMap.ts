
export const mergeToMap = <TKey, TResult, TUpdated>(
    map: Map<TKey, TResult>,
    updated: ReadonlyArray<TUpdated>,
    getKey: (updated: TUpdated, index: number) => TKey,
    {
        create,
        update,
        delete: _delete
    }: {
        create: (data: TUpdated) => TResult;
        update?: (previous: TResult, data: TUpdated) => void;
        delete?: (previous: TResult, key: TKey) => void;
    }
) => {
    const notFoundKeys = new Set([...map.keys()]);
    let index = 0;
    for (const data of updated) {
        const key = getKey(data, index);
        index += 1;

        notFoundKeys.delete(key);
        const existing = map.get(key);
        if (existing) {
            update?.(existing, data);
        }
        else {
            map.set(key, create(data));
        }
    }

    for (const key of notFoundKeys) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        _delete?.(map.get(key)!, key);
        map.delete(key);
    }
};