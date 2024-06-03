import type { Flow, FlowArea, FlowStep } from '../../../../shared/resultTypes';
import type { AreaVisitDetails, FlowDetails } from './detailsTypes';

type Builder<T, TKey extends keyof T> = Omit<T, TKey> & {
    -readonly [K in TKey]: Array<T[K] extends ReadonlyArray<infer TElement> ? TElement : never>;
};

type AreaVisitBuilder = Builder<AreaVisitDetails, 'lines'>;
type ResultBuilder = Builder<FlowDetails, 'lines'|'jumps'>;

const isEndOfAreaVisit = (
    area: FlowArea,
    nextStep: FlowStep,
    nextArea: FlowArea | undefined
): nextArea is undefined => {
    if (!nextArea)
        return true;

    // moved into parent area
    if (nextArea.startLine <= area.startLine && nextArea.endLine >= area.endLine)
        return true;

    if (nextArea.type === 'loop' && (nextArea.startLine >= area.endLine || nextArea.endLine <= area.startLine))
        return true;

    // likely method return
    if (nextArea.type === 'method' && nextStep.line !== nextArea.startLine)
        return true;

    return false;
};

const collectVisitDetailsRecursive = (
    result: ResultBuilder,
    flow: Flow,
    startIndex: number,
    area: FlowArea
) => {
    const startStep = flow.steps[startIndex];
    const visit = {
        type: 'area',
        area,
        order: startIndex,
        start: {
            type: 'step',
            line: startStep.line,
            step: startStep
        },
        lines: []
    } as AreaVisitBuilder;
    result.lines.push(visit);

    return collectLineDetailsRecursive(
        { lines: visit.lines, jumps: result.jumps }, flow, startIndex + 1, area
    );
};

const collectLineDetailsRecursive = (
    result: ResultBuilder,
    flow: Flow,
    startIndex: number,
    area?: FlowArea
) => {
    const { steps, areas } = flow;

    for (let index = startIndex; index < steps.length; index++) {
        const step = steps[index];

        const nextArea = areas
            .filter(a => step.line >= a.startLine && step.line <= a.endLine)
            .sort((a, b) => (a.endLine - a.startLine) - (b.endLine - b.startLine))[0] as FlowArea | undefined;
        if (nextArea !== area) {
            if (area && isEndOfAreaVisit(area, step, nextArea))
                return index - 1;

            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            index = collectVisitDetailsRecursive(result, flow, index, nextArea!);
            continue;
        }

        if (area && step.line === area.startLine)
            return index - 1;

        const details = {
            line: step.line,
            type: 'step',
            step
        } as const;
        result.lines.push(details);
        // eslint-disable-next-line @typescript-eslint/prefer-nullish-coalescing
        if ((step.jump || step.exception) && index < steps.length - 1)
            result.jumps.push({ from: step, to: steps[index + 1], exception: !!step.exception });
    }
    return steps.length;
};

export const processFlowIntoLineDetails = (flow: Flow | null) => {
    const result: ResultBuilder = {
        lines: [],
        jumps: []
    };
    if (!flow)
        return result;

    collectLineDetailsRecursive(result, flow, 0);
    return result;
};