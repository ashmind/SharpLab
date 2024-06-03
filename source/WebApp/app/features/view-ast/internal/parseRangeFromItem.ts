import type { AstItem } from '../../../shared/resultTypes';

export const parseRangeFromItem = (item: AstItem | null) => {
    if (!item?.range)
        return null;

    const [start, end] = item.range.split('-');
    return {
        start: parseInt(start, 10),
        end: parseInt(end, 10)
    };
};