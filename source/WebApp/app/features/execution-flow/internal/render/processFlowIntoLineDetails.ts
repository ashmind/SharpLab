import type { Flow, FlowArea, FlowStep } from '../../../../shared/resultTypes';
import type { JumpDetails } from './JumpDetails';

type Builder<T, TKey extends keyof T> = Omit<T, TKey> & {
    [K in TKey]: Array<T[K] extends ReadonlyArray<infer TElement> ? TElement : never>;
};

export type AreaVisit = {
    readonly start: FlowStep;
    readonly lines: ReadonlyArray<LineDetails>;
};

type AreaVisitBuilder = Builder<AreaVisit, 'lines'>;

export type AreaDetails = {
    readonly line: number;
    readonly type: 'area';
    readonly area: FlowArea;
    readonly visits: ReadonlyArray<AreaVisit>;
};

type AreaBuilder = Builder<AreaDetails, 'visits'>;

export type StepDetails = {
    readonly line: number;
    readonly type: 'step';
    readonly step: FlowStep;
};

export type LineDetails = AreaDetails | StepDetails;

type ResultBuilder = {
    lines: Array<LineDetails>;
    jumps: Array<JumpDetails>;
};

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

const collectAreaVisitRecursive = (
    areaDetails: AreaBuilder,
    jumps: Array<JumpDetails>,
    flow: Flow,
    startIndex: number
) => {
    const visit = {
        start: flow.steps[startIndex],
        lines: []
    } as AreaVisitBuilder;

    areaDetails.visits.push(visit);
    return collectLineDetailsRecursive({ lines: visit.lines, jumps }, flow, startIndex + 1, areaDetails);
};

const collectLineDetailsRecursive = (
    result: ResultBuilder,
    flow: Flow,
    startIndex: number,
    areaDetails?: AreaBuilder
) => {
    const { steps, areas } = flow;
    const area = areaDetails?.area;
    const detailsByArea = new Map<FlowArea, AreaBuilder>();

    let lastStepDetails: StepDetails | null = null;
    for (let index = startIndex; index < steps.length; index++) {
        const step = steps[index];
        const nextArea = areas
            .filter(a => step.line >= a.startLine && step.line <= a.endLine)
            .sort((a, b) => (a.endLine - a.startLine) - (b.endLine - b.startLine))[0] as FlowArea | undefined;
        if (nextArea !== area) {
            if (area && isEndOfAreaVisit(area, step, nextArea))
                return index - 1;

            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            let nextAreaDetails = detailsByArea.get(nextArea!);
            if (!nextAreaDetails) {
                nextAreaDetails = {
                    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                    line: nextArea!.startLine,
                    type: 'area',
                    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                    area: nextArea!,
                    visits: [] as Array<AreaVisit>
                };
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                detailsByArea.set(nextArea!, nextAreaDetails);
                result.lines.push(nextAreaDetails);
            }
            index = collectAreaVisitRecursive(nextAreaDetails, result.jumps, flow, index);
            continue;
        }

        if (area && lastStepDetails && lastStepDetails.line > step.line) {
            index = collectAreaVisitRecursive(areaDetails, result.jumps, flow, index);
            continue;
        }

        const details = {
            line: step.line,
            type: 'step',
            step
        } as const;
        lastStepDetails = details;
        result.lines.push(details);
        if (step.tags?.includes('jump') && index < steps.length - 1)
            result.jumps.push({ from: step, to: steps[index + 1] });
    }
    return steps.length;
};

const inlineSingleVisitAreas = (lines: ReadonlyArray<LineDetails>): Array<LineDetails>  => {
    return lines.flatMap(details => {
        if (details.type !== 'area')
            return details;

        const { visits } = details;
        if (visits.length > 1) {
            return {
                ...details,
                visits: visits.map(visit => ({
                    ...visit,
                    lines: inlineSingleVisitAreas(visit.lines)
                }))
            };
        }

        const { start, lines } = visits[0];
        return [
            { line: start.line, type: 'step', step: start } as const,
            ...inlineSingleVisitAreas(lines)
        ];
    });
};

export const processFlowIntoLineDetails = (flow: Flow | null) => {
    const result: ResultBuilder = {
        lines: [],
        jumps: []
    };
    if (!flow)
        return result;

    collectLineDetailsRecursive(result, flow, 0);
    result.lines = inlineSingleVisitAreas(result.lines);

    console.log('flow', flow);
    console.log('result', result);
    return result;
};