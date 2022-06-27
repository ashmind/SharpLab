import type { FlowStep } from '../../../../shared/resultTypes';

export type JumpDetails = {
    readonly from: FlowStep;
    readonly to: FlowStep;
};