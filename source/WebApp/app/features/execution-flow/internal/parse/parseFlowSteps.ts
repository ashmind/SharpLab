import type { PartiallyMutable } from '../../../../shared/helpers/partiallyMutable';
import type { FlowStep, FlowStepTag } from '../../../../shared/resultTypes';

const TAG_CODE_METHOD_START = 'm';
const TAG_CODE_METHOD_RETURN = 'r';

type ValueTuple = [line: number, value: string, name?: string];
type TagCode = typeof TAG_CODE_METHOD_START | typeof TAG_CODE_METHOD_RETURN;

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
        [TAG_CODE_METHOD_START]: 'method-start',
        [TAG_CODE_METHOD_RETURN]: 'method-return'
    } as const)[code];

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