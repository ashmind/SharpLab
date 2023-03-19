import type { FlowStep } from '../../../../../shared/resultTypes';
import type { JumpDetails } from '../../../../execution-flow/internal/render/detailsTypes';

export type StepForJumps = {
    step: FlowStep;
    mode?: 'any' | 'jump-to-only';
};

export const extractJumpsData = (
    jumps: ReadonlyArray<JumpDetails>,
    steps: ReadonlyArray<StepForJumps>
): ReadonlyArray<JumpData> => {
    const allowedFrom = new Set(steps.filter(s => s.mode !== 'jump-to-only').map(s => s.step));
    const allowedTo = new Set(steps.map(s => s.step));

    return jumps
        .filter(({ from, to }) => allowedFrom.has(from) && allowedTo.has(to))
        .map(({ from, to, exception }) => ({
            fromLine: from.line,
            toLine: to.line,
            options: { throw: exception }
        }));
};