import type { FlowStep, OutputItem } from '../types/results';
import type { PartiallyMutable } from './partially-mutable';

type Inspection = Exclude<OutputItem, string>;
type FlowMatchesGroups = { lineNumber: number }
                       | { lineNumber: number; value: string; name?: string }
                       | { exception: string };
type FlowStepBuilder = Omit<PartiallyMutable<FlowStep, 'notes'|'exception'>, 'skipped'> & {
    skipped: boolean;
};

export default function parseOutput(outputString: string) {
    const flow = [] as Array<FlowStepBuilder>;
    const output = [] as Array<OutputItem>;

    for (const line of outputString.split(/\r\n|\r|\n/g)) {
        if (line.startsWith('#{')) {
            output.push(parseAsOutputItem(line));
            continue;
        }

        const flowMatches = line.match(
            /^#fl:(?:(?<lineNumber>\d+)(?::(?<name>[^:]*):(?<value>.*))?|e:(?<exception>.*))$/
        );
        if (flowMatches) {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            addAsFlowStep(flow, flowMatches.groups! as FlowMatchesGroups);
            continue;
        }

        output.push(line);
    }

    return { output, flow };
}

function parseAsOutputItem(line: string) {
    const json = line.substr(1);
    try {
        const candidate = JSON.parse(json) as Inspection | { type?: undefined };
        return (candidate.type && candidate.type.startsWith('inspection:')) ? candidate : line;
    }
    catch {
        return line;
    }
}

function addAsFlowStep(
    flow: Array<FlowStepBuilder>,
    { lineNumber, name, value, exception }: { lineNumber?: number; name?: string; value?: string; exception?: string }
) {
    if (exception) {
        if (flow.length === 0)
            return;
        flow[flow.length - 1].exception = exception;
        return;
    }

    let step = flow.find(s => s.line === lineNumber);
    if (!step) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        step = { line: lineNumber!, skipped: !!value, notes: '' };
        flow.push(step);
    }

    if (value) {
        if (step.notes)
            step.notes += ', ';
        if (name)
            step.notes += name + ': ';
        step.notes += value;
    }
    else {
        step.skipped = false;
    }
}