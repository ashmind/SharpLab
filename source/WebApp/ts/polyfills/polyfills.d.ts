interface ObjectConstructor {
    fromEntries<T, TKey extends PropertyKey>(entries: Iterable<readonly [TKey, T]>): { [k in TKey]: T };
}