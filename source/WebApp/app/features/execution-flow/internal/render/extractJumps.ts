import type { FlowStepForJumps } from './FlowStepForJumps';

export const extractJumps = (steps: ReadonlyArray<FlowStepForJumps>) => {
    const jumps = [] as Array<JumpData>;
    let last: FlowStepForJumps | undefined;
    for (const step of steps) {
        if (step.skipped)
            continue;
        const { line } = step;

        const important = (last?.line != null && (line < last.line || line - last.line > 2)) || last?.exception;
        if (important && !last?.ignoreForJumpsOut) {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            jumps.push({ fromLine: last!.line - 1, toLine: line - 1, options: { throw: !!last!.exception } });
        }
        last = step;
    }
    return jumps;
};