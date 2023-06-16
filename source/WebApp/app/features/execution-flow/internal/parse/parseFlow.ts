import type { PartiallyMutable } from '../../../../shared/helpers/partiallyMutable';
import type { Flow, FlowArea, FlowStep } from '../../../../shared/resultTypes';

type ValueTuple = [line: number, value: string, name?: string];
type AreaTuple = [code: string, startLine: number, endLine: number];
type TagCode = string;

export type OutputJsonLineFlow = {
    readonly flow: ReadonlyArray<
        number
            | AreaTuple
            | ValueTuple
            | TagCode
            | { exception: string }
    >;
};

type FlowStepBuilder = PartiallyMutable<FlowStep, 'notes'|'exception'|'skipped'|'jump'>;

type FlowBuilder = {
    steps: Array<FlowStepBuilder>;
    areas: Map<string, FlowArea>;
};

const addFlowArea = (
    { areas }: FlowBuilder,
    [code, startLine, endLine]: AreaTuple
) => {
    const type = ({
        m: 'method',
        l: 'loop'
    } as const)[code] ?? `unknown: ${code}`;

    const key = `${type}-${startLine}-${endLine}`;
    // loops are reported in each method rather
    // than centrally, so they might be duplicated
    // in output.
    if (areas.has(key))
        return;

    areas.set(key, {
        type,
        startLine,
        endLine
    });
};

const addFlowLine = (
    { steps }: FlowBuilder,
    line: number
) => {
    const previous = steps[steps.length - 1];
    if (previous?.line === line) {
        previous.skipped = false;
        return;
    }

    const step = { line };
    steps.push(step);
};

const addFlowValue = (
    { steps }: FlowBuilder,
    [line, value, name]: ValueTuple
) => {
    let step = steps[steps.length - 1];
    if (step?.line !== line) {
        step = { line, skipped: true, notes: '' };
        steps.push(step);
    }

    if (step.notes)
        step.notes += ', ';
    step.notes ??= '';
    if (name)
        step.notes += name + ': ';
    step.notes += value;
};

const addFlowJump = ({ steps }: FlowBuilder) => {
    if (steps.length === 0)
        return;

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    steps[steps.length - 1]!.jump = true;
};

const addFlowException = (
    { steps }: FlowBuilder,
    { exception }: { exception: string }
) => {
    if (steps.length === 0)
        return;

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    steps[steps.length - 1]!.exception = exception;
};

export const parseFlow = (data: OutputJsonLineFlow['flow']): Flow => {
    const flow: FlowBuilder = {
        steps: [],
        areas: new Map()
    };

    for (const item of data) {
        if (typeof item === 'number') {
            addFlowLine(flow, item);
            continue;
        }

        if (item === 'j') {
            addFlowJump(flow);
            continue;
        }

        if (Array.isArray(item)) {
            if (typeof item[0] === 'number') {
                addFlowValue(flow, item as ValueTuple);
            }
            else if (typeof item[0] === 'string') {
                addFlowArea(flow, item as AreaTuple);
            }
            continue;
        }

        if (typeof item === 'object' && 'exception' in item) {
            addFlowException(flow, item);
            continue;
        }
    }

    return {
        steps: flow.steps,
        areas: [...flow.areas.values()]
    };
};