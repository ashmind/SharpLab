import type { FlowStep, FlowStepTag } from '../../../../shared/resultTypes';
import { FLOW_TAG_LOOP_END, FLOW_TAG_LOOP_START, FLOW_TAG_METHOD_RETURN, FLOW_TAG_METHOD_START } from '../tags';

export type Visit = {
    readonly start: FlowStep;
    readonly end: FlowStep;
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

export type SimpleLineDetails = {
    readonly line: number;
    readonly type: 'step';
    readonly step: FlowStep;
    readonly jumpTo?: FlowStep;
};

export type LineDetails = MethodDetails | LoopDetails | SimpleLineDetails;

const isEndStepFromOuterArea = (step: FlowStep) => {
    return !!step.tags?.includes(FLOW_TAG_LOOP_END)
        || !!step.tags?.includes(FLOW_TAG_METHOD_RETURN);
};

const collectAreaVisit = <TAreaBuilder extends MethodDetailsBuilder | LoopDetailsBuilder>(
    results: Array<LineDetails>,
    steps: ReadonlyArray<FlowStep>,
    type: TAreaBuilder['type'],
    mapByStartLine: Map<number, TAreaBuilder>,
    startIndex: number,
    endTag: FlowStepTag
) => {
    const start = steps[startIndex];
    const { line } = start;

    let area = mapByStartLine.get(line);
    const visitLines = [] as Array<LineDetails>;

    const isEndStep = (step: FlowStep, index: number) => {
        if (step.tags?.includes(endTag))
            return true;

        if (!area?.visits.length)
            return false;

        const { start: areaStart, end: areaEnd } = area.visits[0];
        const next = steps[index + 1] as FlowStep | undefined;
        if (!next)
            return false;

        return next.line < areaStart.line
            || next.line > areaEnd.line
            || isEndStepFromOuterArea(next);
    };
    const [endIndex, end] = collectLineDetailsRecursive(
        visitLines, steps, startIndex + 1, isEndStep
    );
    if (!end && !area) {
        // end not found -- inline visit
        results.push(...visitLines);
        return endIndex;
    }

    if (!area) {
        area = { line, type, visits: [] as Array<Visit> } as TAreaBuilder;
        mapByStartLine.set(line, area);
        results.push(area);
    }

    area.visits.push({
        start,
        end: end ?? steps[steps.length - 1],
        lines: visitLines
    });

    return endIndex;
};

const getJumpTarget = (steps: ReadonlyArray<FlowStep>, index: number) => {
    if (steps[index + 1]?.tags?.includes(FLOW_TAG_METHOD_START))
        return steps[index + 1];

    const tags = steps[index].tags;
    if (tags?.some(t => t === FLOW_TAG_LOOP_END || t === FLOW_TAG_METHOD_RETURN) && index < steps.length)
        return steps[index + 1];
};

const collectLineDetailsRecursive = (
    results: Array<LineDetails>,
    steps: ReadonlyArray<FlowStep>,
    startIndex: number,
    isLastStepToCollect: (step: FlowStep, index: number) => boolean
) => {
    const methodsByStartLine = new Map<number, MethodDetailsBuilder>();
    const loopsByStartLine = new Map<number, LoopDetailsBuilder>();

    for (let index = startIndex; index < steps.length; index++) {
        const step = steps[index];

        if (step.tags?.includes(FLOW_TAG_METHOD_START)) {
            index = collectAreaVisit(results, steps, 'method', methodsByStartLine, index, FLOW_TAG_METHOD_RETURN);
            continue;
        }

        if (step.tags?.includes(FLOW_TAG_LOOP_START)) {
            index = collectAreaVisit(results, steps, 'loop', loopsByStartLine, index, FLOW_TAG_LOOP_END);
            continue;
        }

        results.push({
            line: step.line,
            type: 'step',
            step,
            jumpTo: getJumpTarget(steps, index)
        });

        if (isLastStepToCollect(step, index))
            return [index, step] as const;
    }
    return [steps.length, null] as const;
};

export const processStepsIntoLineDetails = (steps: ReadonlyArray<FlowStep>) => {
    const results = [] as Array<LineDetails>;
    collectLineDetailsRecursive(results, steps, 0, () => false);

    console.log('steps', steps);
    console.log('results', results);
    return results;
};