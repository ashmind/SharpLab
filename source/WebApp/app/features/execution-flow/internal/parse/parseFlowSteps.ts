import type { PartiallyMutable } from '../../../../shared/helpers/partiallyMutable';
import type { FlowStep, FlowStepTag } from '../../../../shared/resultTypes';
import { FLOW_TAG_LOOP_END, FLOW_TAG_LOOP_START, FLOW_TAG_METHOD_RETURN, FLOW_TAG_METHOD_START } from '../tags';

type ValueTuple = [line: number, value: string, name?: string];
type TagCode = string;

export type OutputJsonLineFlow = {
    readonly flow: ReadonlyArray<
        number
            | ValueTuple
            | TagCode
            | { exception: string }
    >;
};

type FlowStepBuilder = Omit<PartiallyMutable<FlowStep, 'notes'|'exception'>, 'skipped'|'tags'> & {
    tags?: Array<FlowStepTag>;
    skipped?: boolean;
};

const addFlowLine = (
    flow: Array<FlowStepBuilder>,
    line: number
) => {
    const previous = flow[flow.length - 1] as FlowStepBuilder | undefined;
    if (previous?.line === line) {
        previous.skipped = false;
        return;
    }

    const step = { line };
    flow.push(step);
};

const addFlowValue = (
    flow: Array<FlowStepBuilder>,
    [line, value, name]: ValueTuple
) => {
    let step = flow[flow.length - 1] as FlowStepBuilder | undefined;
    if (step?.line !== line) {
        step = { line, skipped: true, notes: '' };
        flow.push(step);
    }

    if (step.notes)
        step.notes += ', ';
    step.notes ??= '';
    if (name)
        step.notes += name + ': ';
    step.notes += value;
};

const addFlowTag = (flow: Array<FlowStepBuilder>, code: TagCode) => {
    if (flow.length === 0)
        return;

    const tag = ({
        m: FLOW_TAG_METHOD_START,
        r: FLOW_TAG_METHOD_RETURN,
        ls: FLOW_TAG_LOOP_START,
        le: FLOW_TAG_LOOP_END
    } as const)[code] ?? `unknown: ${code}`;

    const previous = flow[flow.length - 1];
    previous.tags ??= [];
    previous.tags.push(tag);
};

const addFlowException = (
    flow: Array<FlowStepBuilder>,
    { exception }: { exception: string }
) => {
    if (flow.length === 0)
        return;

    flow[flow.length - 1].exception = exception;
};

export const parseFlowSteps = (data: OutputJsonLineFlow['flow']) => {
    console.log('flow', data);
    const flow = [] as Array<FlowStepBuilder>;

    for (const item of data) {
        if (typeof item === 'number') {
            addFlowLine(flow, item);
            continue;
        }

        if (typeof item === 'string') {
            addFlowTag(flow, item);
            continue;
        }

        if (Array.isArray(item)) {
            addFlowValue(flow, item);
            continue;
        }

        if (typeof item === 'object' && 'exception' in item) {
            addFlowException(flow, item);
            continue;
        }
    }

    return flow as ReadonlyArray<FlowStep>;
};