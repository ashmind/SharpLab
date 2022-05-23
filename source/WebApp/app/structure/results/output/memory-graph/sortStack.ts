import type { MemoryGraphStackNode } from '../../../../shared/resultTypes';

export type SortedStackNode = (MemoryGraphStackNode & { isSeparator?: undefined }) | {
    isSeparator: true;
    size: number;
};

export const sortStack = (stack: ReadonlyArray<MemoryGraphStackNode>) => {
    const nodes = stack.slice(0);
    nodes.sort((a, b) => {
        if (a.offset > b.offset) return 1;
        if (a.offset < b.offset) return -1;
        return 0;
    });
    const entries = [];
    let last = null;
    for (const node of nodes) {
        const separatorSize = last ? node.offset - (last.offset + last.size) : 0;
        if (separatorSize > 0)
            entries.push({ isSeparator: true, size: separatorSize } as const);
        entries.push(node);
        last = node;
    }
    return entries;
};