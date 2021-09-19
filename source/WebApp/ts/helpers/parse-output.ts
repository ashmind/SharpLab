import type { FlowStep, OutputItem, OutputJsonLineFlow } from '../types/results';
import type { PartiallyMutable } from './partially-mutable';

type OutputJsonLineData = Exclude<OutputItem, string> | OutputJsonLineFlow;
type FlowStepBuilder = Omit<PartiallyMutable<FlowStep, 'notes'|'exception'>, 'skipped'> & {
    skipped: boolean;
};

export default function parseOutput(outputString: string) {
    const output = [] as Array<OutputItem>;
    let flow = [] as ReadonlyArray<FlowStep>;

    let lastIndex = 0;
    const commitFragmentUpTo = (index: number) => {
        if (index === lastIndex)
            return;

        output.push(outputString.substring(lastIndex, index));
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        lastIndex = index;
    };

    for (const match of outputString.matchAll(/#(\{[^\n]+)\n/g)) {
        try {
            const json = match[1];
            const candidate = JSON.parse(json) as OutputJsonLineData;
            if ('type' in candidate && candidate.type.startsWith('inspection:')) {
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                commitFragmentUpTo(match.index!);
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                lastIndex += match[0]!.length;
                output.push(candidate);
                continue;
            }

            if ('flow' in candidate) {
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                commitFragmentUpTo(match.index!);
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                lastIndex += match[0]!.length;
                flow = normalizeFlow(candidate.flow);
                continue;
            }
        }
        // eslint-disable-next-line no-empty
        catch {
        }
    }

    commitFragmentUpTo(outputString.length);

    return { output, flow };
}

function normalizeFlow(data: OutputJsonLineFlow['flow']) {
    const flow = [] as Array<FlowStepBuilder>;

    for (const item of data) {
        if (typeof item === 'number') {
            addFlowLine(flow, item);
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
}

function addFlowLine(
    flow: Array<FlowStepBuilder>,
    line: number
) {
    let step = flow[flow.length - 1] as FlowStepBuilder|undefined;
    if (step && step.line === line) {
        step.skipped = false;
        return;
    }

    step = { line, skipped: false, notes: '' };
    flow.push(step);
}

function addFlowValue(
    flow: Array<FlowStepBuilder>,
    [line, value, name]: [number, string, string?]
) {
    let step = flow.slice().reverse().find(s => s.line === line);
    if (!step) {
        step = { line, skipped: true, notes: '' };
        flow.push(step);
    }

    if (step.notes)
        step.notes += ', ';
    if (name)
        step.notes += name + ': ';
    step.notes += value;
}

function addFlowException(
    flow: Array<FlowStepBuilder>,
    { exception }: { exception: string }
) {
    if (flow.length === 0)
        return;

    flow[flow.length - 1].exception = exception;
}