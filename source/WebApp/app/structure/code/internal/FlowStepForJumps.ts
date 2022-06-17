import type { FlowStep } from '../../../shared/resultTypes';

export type FlowStepForJumps = FlowStep & {
    ignoreForJumpsOut?: boolean;
};