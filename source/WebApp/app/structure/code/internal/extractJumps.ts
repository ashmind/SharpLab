import type { FlowStep } from '../../../shared/resultTypes';

export const extractJumps = (steps: ReadonlyArray<FlowStep>) => {
    const jumps = [] as Array<JumpData>;
    let lastLineNumber: number|undefined;
    let lastException: string|undefined|null;
    for (const step of steps) {
        if (step.skipped)
            continue;
        const { line: lineNumber, exception } = step;

        const important = (lastLineNumber != null && (lineNumber < lastLineNumber || lineNumber - lastLineNumber > 2)) || lastException;
        if (important) {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            jumps.push({ fromLine: lastLineNumber! - 1, toLine: lineNumber - 1, options: { throw: !!lastException } });
        }
        lastLineNumber = lineNumber;
        lastException = exception;
    }
    return jumps;
};