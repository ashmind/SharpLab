import type { NodeRect } from './types';

export const getConnectionPoints = (from: NodeRect, to: NodeRect, { allowVertical }: { allowVertical: boolean }) => {
    // from inside to
    if (from.top >= to.top && from.bottom <= to.bottom && from.left >= to.left && from.right <= to.right) {
        return {
            from: { x: from.right, y: from.top + (from.height / 2) },
            to: { x: to.left + (to.width / 2), y: to.top },
            arc: true
        };
    }

    const horizontalOffset = to.left > from.left ? (to.left - from.left) : (from.left - to.left);
    // to below from
    if (allowVertical && to.top > from.bottom && to.top - from.bottom > horizontalOffset) {
        return {
            from: { x: from.left + (from.width / 2), y: from.bottom },
            to:   { x: to.left + (to.width / 2),     y: to.top }
        };
    }

    // to above from
    if (to.bottom < from.top && from.top - to.bottom > horizontalOffset) {
        return {
            from: { x: from.right, y: from.top + (from.height / 2) },
            to:   { x: to.left + (to.width / 2), y: to.bottom }
        };
    }

    if (to.right < from.left) {
        return {
            from: { x: from.left, y: from.top + (from.height / 2) },
            to:   { x: to.right,  y: to.top + (to.height / 2) }
        };
    }

    return {
        from: { x: from.right, y: from.top + (from.height / 2) },
        to:   { x: to.left,    y: to.top + (to.height / 2) }
    };
};