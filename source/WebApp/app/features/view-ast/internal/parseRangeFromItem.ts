import type { AstItem } from '../../../shared/resultTypes';

export const parseRangeFromItem = (item: AstItem | null) => {
    if (!item || !item.range)
        return null;

    const [start, end] = item.range.split('-');
    return {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        start: parseInt(start!, 10),
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        end: parseInt(end!, 10)
    };
};