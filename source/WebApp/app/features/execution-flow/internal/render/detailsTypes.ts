import type { FlowArea, FlowStep } from '../../../../shared/resultTypes';

export type AreaVisitDetails = {
    readonly type: 'area';
    readonly area: FlowArea;
    readonly start: StepDetails;
    readonly lines: ReadonlyArray<LineDetails>;
    readonly order: number;
};

export type StepDetails = {
    readonly line: number;
    readonly type: 'step';
    readonly step: FlowStep;
};

export type LineDetails = StepDetails | AreaVisitDetails;

export type JumpDetails = {
    readonly from: FlowStep;
    readonly to: FlowStep;
};

export type FlowDetails = {
    readonly lines: ReadonlyArray<LineDetails>;
    readonly jumps: ReadonlyArray<JumpDetails>;
};