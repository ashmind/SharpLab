import type { FlowStep } from '../../../shared/resultTypes';

export type LoopVisit = {
    start: FlowStep;
    lines?: Array<LineDetails>;
};

export type LoopDetails = {
    line: number;
    endLine: number;
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
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const endLine = previous!.line;
    do {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        loopLines.push(previous!);
        results.pop();
        previous = results[results.length - 1] as LineDetails | undefined;
    } while (previous && start.line < previous.line);

    loopLines.reverse();

    if (previous && start.line === previous.line) {
        if (previous.type !== 'loop') {
            results.pop();
            previous = {
                line: start.line,
                endLine,
                type: 'loop',
                visits: [{ start: previous.step }]
            };
            results.push(previous);
        }

        previous.visits[previous.visits.length - 1].lines = loopLines;
        previous.visits.push({ start });
        return previous;
    }

    const loop: LoopDetails = {
        line: start.line,
        endLine,
        type: 'loop',
        visits: [
            { start: { line: start.line }, lines: loopLines },
            { start }
        ]
    };
    results.push(loop);
    return loop;
};

export const processStepsIntoLineDetails = (steps: ReadonlyArray<FlowStep>) => {
    const results = new Array<LineDetails>();

    let currentLoop = null as LoopDetails | null;
    for (const step of steps) {
        const previous = results[results.length - 1] as LineDetails | undefined;
        if (previous && step.line <= previous.line) {
            // loop
            currentLoop = collectLoop(step, results);
            continue;
        }

        // TODO: Nested loops
        if (currentLoop && step.line > currentLoop.endLine && currentLoop !== previous) {
            const currentVisit = currentLoop.visits[currentLoop.visits.length - 1];
            const currentLoopIndex = results.indexOf(currentLoop);
            currentVisit.lines = results.splice(currentLoopIndex + 1, results.length - (currentLoopIndex + 1));
            currentLoop = null;
        }

        results.push({
            line: step.line,
            type: 'step',
            step
        });
    }

    return results;
};