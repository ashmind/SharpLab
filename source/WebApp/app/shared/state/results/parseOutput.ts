import { tryParseOutputJsonAsFlow } from '../../../features/execution-flow/tryParseOutputJsonAsFlow';
import type { Flow, OutputItem } from '../../resultTypes';

type OutputJsonLineData = Exclude<OutputItem, string> | object;

export const parseOutput = (outputString: string) => {
    const output = [] as Array<OutputItem>;
    let flow: Flow | null = null;

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
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            const json = match[1]!;
            const candidate = JSON.parse(json) as OutputJsonLineData;
            if ('type' in candidate && candidate.type.startsWith('inspection:')) {
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                commitFragmentUpTo(match.index!);
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                lastIndex += match[0]!.length;
                output.push(candidate);
                continue;
            }

            const flowCandidate = tryParseOutputJsonAsFlow(candidate);
            if (flowCandidate) {
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                commitFragmentUpTo(match.index!);
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                lastIndex += match[0]!.length;
                flow = flowCandidate;
                continue;
            }
        }
        // eslint-disable-next-line no-empty
        catch {
        }
    }

    commitFragmentUpTo(outputString.length);

    return { output, flow };
};