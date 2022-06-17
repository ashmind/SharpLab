import type { FlowStep } from '../../../shared/resultTypes';

export type LoopVisit = {
    start: FlowStep;
    lines?: Array<LineDetails>;
};

export type LoopDetails = {
    line: number;
    type: 'loop';
    visits: Array<LoopVisit>;
};

export type LineDetails = LoopDetails | {
    line: number;
    type: 'step';
    step: FlowStep;
};

const collectLoop = (start: FlowStep, results: Array<LineDetails>) => {
    const loopLines = [] as Array<LineDetails>;
    let previous = results[results.length - 1] as LineDetails | undefined;
    while (previous && start.line < previous.line) {
        loopLines.push(previous);
        results.pop();
        previous = results[results.length - 1] as LineDetails | undefined;
    }
    loopLines.reverse();
    if (previous && start.line === previous.line) {
        if (previous.type !== 'loop') {
            results.pop();
            previous = {
                line: start.line,
                type: 'loop',
                visits: [{ start: previous.step }]
            };
            results.push(previous);
        }

        previous.visits[previous.visits.length - 1].lines = loopLines;
        previous.visits.push({ start });
        return;
    }

    results.push({
        line: start.line,
        type: 'loop',
        visits: [
            { start: { line: start.line }, lines: loopLines },
            { start }
        ]
    });
};

export const processStepsIntoLineDetails = (steps: ReadonlyArray<FlowStep>) => {
    const results = new Array<LineDetails>();
    for (const step of steps) {
        const previous = results[results.length - 1] as LineDetails | undefined;
        if (previous && step.line <= previous.line) {
            // loop
            collectLoop(step, results);
            continue;
        }

        results.push({
            line: step.line,
            type: 'step',
            step
        });
    }

    return results;
};