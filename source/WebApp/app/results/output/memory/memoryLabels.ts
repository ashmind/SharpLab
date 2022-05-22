import type { MemoryInspectionLabel } from '../../../shared/resultTypes';

type InterimLabel = MemoryInspectionLabel & {
    levelSpan?: number;
    levelSpanPlaceholder?: never;
};

interface SpanPlaceholder {
    readonly offset: number;
    readonly length: number;
    readonly nested?: never;

    levelSpan?: number;
    levelSpanPlaceholder: true;
}

export interface FinalLabel {
    readonly name?: string;
    readonly offset: number;
    readonly length: number;

    // TODO: Confirm this actually exists -- Vue templates had it,
    // but type itself didn't, so maybe it's not populated.
    readonly levelSpan?: number;
}

const addLabelsToLevelRecursive = (levels: Array<Array<InterimLabel>>, labels: ReadonlyArray<MemoryInspectionLabel>, index: number) => {
    let level = levels[index] as Array<InterimLabel>|undefined;
    if (!level) {
        level = [];
        levels[index] = level;
    }
    for (const { name, offset, length, nested } of labels) {
        level.push({ name, offset, length, nested });
        if (nested && nested.length > 0)
            addLabelsToLevelRecursive(levels, nested, index + 1);
    }
};

const applyCrossLevelSpansToLabels = (levels: ReadonlyArray<Array<InterimLabel|SpanPlaceholder>>) => {
    for (let i = 0; i < levels.length; i++) {
        for (const label of levels[i]) {
            if (label.nested && label.nested.length > 0)
                continue;
            label.levelSpan = 1;
            for (let j = i + 1; j < levels.length; j++) {
                levels[j].push({ offset: label.offset, length: label.length, levelSpanPlaceholder: true });
                label.levelSpan += 1;
            }
        }
    }
};

const sortAndAddPaddingBetweenLabels = (labels: ReadonlyArray<InterimLabel|SpanPlaceholder>, dataLength: number) => {
    const results = [] as Array<FinalLabel>;
    labels = labels.slice(0).sort((a, b) => {
        if (a.offset > b.offset) return +1;
        if (a.offset < b.offset) return -1;
        return 0;
    });

    for (let i = 0; i < labels.length; i++) {
        const label = labels[i];
        if (i === 0 && label.offset > 0)
            results.push({ offset: 0, length: label.offset });

        if (!label.levelSpanPlaceholder)
            results.push(label);

        const next = (labels[i + 1] ) ?? { offset: dataLength };
        const offset = label.offset + label.length;
        const padding = { offset, length: next.offset - offset };
        if (padding.length > 0)
            results.push(padding);
    }
    return results;
};

export const calculateLabelLevels = (labels: ReadonlyArray<MemoryInspectionLabel>, dataLength: number) => {
    const levels = [] as Array<Array<InterimLabel|SpanPlaceholder>>;
    addLabelsToLevelRecursive(levels as Array<Array<InterimLabel>>, labels, 0);
    applyCrossLevelSpansToLabels(levels);

    return levels.map(level => sortAndAddPaddingBetweenLabels(level, dataLength));
};