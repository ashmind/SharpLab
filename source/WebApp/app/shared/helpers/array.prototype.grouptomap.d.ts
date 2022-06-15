declare module 'array.prototype.grouptomap' {
    export default function groupToMap<TItem, TKey>(
        array: ReadonlyArray<TItem>, key: (item: TItem) => TKey
    ): Map<TKey, Array<TItem>>;
}