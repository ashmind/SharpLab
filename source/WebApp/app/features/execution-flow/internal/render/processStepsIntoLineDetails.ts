import type { FlowStep } from '../../../../shared/resultTypes';

export type Visit = {
    readonly start: FlowStep;
    readonly lines: ReadonlyArray<LineDetails>;
};

export type RepeatAreaDetails = {
    readonly line: number;
    readonly visits: ReadonlyArray<Visit>;
};
export type MethodDetails = RepeatAreaDetails & { readonly type: 'method' };
export type LoopDetails = RepeatAreaDetails & { readonly type: 'loop' };

type RepeatAreaBuilder = {
    line: number;
    visits: Array<Visit>;
};
type MethodDetailsBuilder = RepeatAreaBuilder & { type: 'method' };
type LoopDetailsBuilder = RepeatAreaBuilder & { type: 'loop' };

export type LineDetails = MethodDetails | LoopDetails | {
    readonly line: number;
    readonly type: 'step';
    readonly step: FlowStep;
};

const prepareAreaVisit = <TType, TAreaBuilder extends RepeatAreaBuilder & { type: TType }>(
    type: TType,
    mapByStartLine: Map<number, TAreaBuilder>,
    start: FlowStep
) => {
    const { line } = start;

    let area = mapByStartLine.get(line);
    let areaIsNew = false;
    if (!area) {
        area = { line, type, visits: [] as Array<Visit> } as TAreaBuilder;
        mapByStartLine.set(line, area);
        areaIsNew = true;
    }

    const visit = { start, lines: [] };
    area.visits.push(visit);

    return [visit, area, areaIsNew] as const;
};

const collectLineDetailsRecursive = (
    results: Array<LineDetails>,
    steps: ReadonlyArray<FlowStep>,
    startIndex: number,
    isLastStepToCollect: (step: FlowStep) => boolean,
    context: {
        readonly methodsByStartLine: Map<number, MethodDetailsBuilder>;
        readonly loopsByStartLine: Map<number, LoopDetailsBuilder>;
    }
) => {
    for (let index = startIndex; index < steps.length; index++) {
        const step = steps[index];

        if (step.tags?.includes('method-start')) {
            const [visit] = prepareAreaVisit('method', context.methodsByStartLine, step);
            index = collectLineDetailsRecursive(
                visit.lines, steps,
                index + 1, s => !!s.tags?.includes('method-return'),
                context
            );
            continue;
        }

        if (step.tags?.includes('loop-start')) {
            const [visit, loop, loopIsNew] = prepareAreaVisit('loop', context.loopsByStartLine, step);
            if (loopIsNew)
                results.push(loop);
            index = collectLineDetailsRecursive(
                visit.lines, steps,
                index + 1, s => !!s.tags?.includes('loop-end'),
                context
            );
            continue;
        }

        results.push({
            line: step.line,
            type: 'step',
            step
        });

        if (isLastStepToCollect(step))
            return index;
    }
    return steps.length - 1;
};

export const processStepsIntoLineDetails = (steps: ReadonlyArray<FlowStep>) => {
    const results = [] as Array<LineDetails>;
    const methodsByStartLine = new Map<number, MethodDetailsBuilder>();
    const loopsByStartLine = new Map<number, LoopDetailsBuilder>();

    collectLineDetailsRecursive(results, steps, 0, () => false, {
        methodsByStartLine,
        loopsByStartLine
    });
    for (const method of methodsByStartLine.values()) {
        results.push(method);
    }

    console.log('steps', steps);
    console.log('results', results);
    return results;
};