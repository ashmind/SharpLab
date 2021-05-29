import type { FlowStep, OutputItem, OutputJsonLineFlow } from '../types/results';
import type { PartiallyMutable } from './partially-mutable';

type JsonLineData = Exclude<OutputItem, string> | OutputJsonLineFlow;
type FlowStepBuilder = Omit<PartiallyMutable<FlowStep, 'notes'|'exception'>, 'skipped'> & {
    skipped: boolean;
};

export default function parseOutput(outputString: string) {
    const output = [] as Array<OutputItem>;
    let flow = [] as ReadonlyArray<FlowStep>;

    for (const line of outputString.split(/\r\n|\r|\n/g)) {
        if (!line.startsWith('#{')) {
            output.push(line);
            continue;
        }

        const json = line.substr(1);
        try {
            const candidate = JSON.parse(json) as JsonLineData;
            if ('type' in candidate && candidate.type.startsWith('inspection:')) {
                output.push(candidate);
                continue;
            }

            if ('flow' in candidate) {
                flow = normalizeFlow(candidate.flow);
                continue;
            }
        }
        // eslint-disable-next-line no-empty
        catch {
        }

        output.push(line);
    }

    return { output, flow };
}

function normalizeFlow(data: OutputJsonLineFlow['flow']) {
    const flow = [] as Array<FlowStepBuilder>;

    for (const item of data) {
        if (typeof item === 'number') {
            let step = flow[flow.length - 1] as FlowStepBuilder|undefined;
            if (step && step.line === item) {
                step.skipped = false;
                continue;
            }

            step = { line: item, skipped: false, notes: '' };
            flow.push(step);
            continue;
        }

        if ('exception' in item) {
            if (flow.length === 0)
                continue;
            flow[flow.length - 1].exception = item.exception;
            continue;
        }

        let step = flow.slice().reverse().find(s => s.line === item.line);
        if (!step) {
            step = { line: item.line, skipped: true, notes: '' };
            flow.push(step);
        }

        if (step.notes)
            step.notes += ', ';
        if (item.name)
            step.notes += item.name + ': ';
        step.notes += item.value;
    }

    return flow as ReadonlyArray<FlowStep>;
}