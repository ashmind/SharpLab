import type { AstItem } from '../../../shared/resultTypes';

const matchesOffset = (item: AstItem, offset: number) => {
    if (!item.range)
        return false;
    const [start, end] = item.range.split('-');
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    return offset >= parseInt(start!, 10)
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        && offset <= parseInt(end!, 10);
};

export const findItemPathByOffset = (items: ReadonlyArray<AstItem>, offset: number): ReadonlyArray<AstItem> | null => {
    const matching = items.find(item => matchesOffset(item, offset));
    if (!matching)
        return null;

    const { children } = matching;
    if (children?.length) {
        const matchingDescendantPath = findItemPathByOffset(children, offset);
        if (matchingDescendantPath)
            return [matching, ...matchingDescendantPath];
    }
    return [matching];
};