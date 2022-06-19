import type { FlowStep } from '../../../../shared/resultTypes';

export type Visit = {
    readonly start: FlowStep;
    readonly lines?: ReadonlyArray<LineDetails>;
};

export type RepeatAreaDetails = {
    readonly line: number;
    readonly visits: ReadonlyArray<Visit>;
};

export type MethodDetails = RepeatAreaDetails & {
    readonly type: 'method';
};

export type LoopDetails = RepeatAreaDetails & {
    readonly type: 'loop';
};

type MethodDetailsBuilder = {
    line: number;
    type: 'method';
    visits: Array<Visit>;
};

type LoopDetailsBuilder = {
    line: number;
    type: 'loop';
    visits: Array<Visit>;
};

export type LineDetails = MethodDetails | LoopDetails | {
    readonly line: number;
    readonly type: 'step';
    readonly step: FlowStep;
};

/*
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
*/

const collectRepeatAreaDetails = () => {

};

const collectLineDetailsRecursive = (
    results: Array<LineDetails>,
    steps: ReadonlyArray<FlowStep>,
    startIndex: number,
    isLastStepToCollect: (step: FlowStep) => boolean,
    context: {
        readonly methodsByStartLine: Map<number, MethodDetailsBuilder>;
        readonly loopsByStartLine: Map<number, LoopDetailsBuilder>;
    }
) => {
    for (let index = startIndex; index < steps.length; index++) {
        const step = steps[index];

        if (step.tags?.includes('method-start')) {
            let method = context.methodsByStartLine.get(step.line);
            if (!method) {
                method = {
                    line: step.line,
                    type: 'method',
                    visits: []
                };
                context.methodsByStartLine.set(step.line, method);
            }

            const visit = { start: step, lines: [] };
            method.visits.push(visit);
            index = collectLineDetailsRecursive(
                visit.lines, steps,
                index + 1, s => !!s.tags?.includes('method-return'),
                context
            );
            continue;
        }

        if (step.tags?.includes('loop-start')) {
            let loop = context.loopsByStartLine.get(step.line);
            if (!loop) {
                loop = {
                    line: step.line,
                    type: 'loop',
                    visits: []
                };
                context.loopsByStartLine.set(step.line, loop);
                results.push(loop);
            }

            const visit = { start: step, lines: [] };
            loop.visits.push(visit);
            index = collectLineDetailsRecursive(
                visit.lines, steps,
                index + 1, s => !!s.tags?.includes('loop-end'),
                context
            );
            continue;
        }

        results.push({
            line: step.line,
            type: 'step',
            step
        });

        if (isLastStepToCollect(step))
            return index;
    }
    return steps.length - 1;
};

export const processStepsIntoLineDetails = (steps: ReadonlyArray<FlowStep>) => {
    const results = [] as Array<LineDetails>;
    const methodsByStartLine = new Map<number, MethodDetailsBuilder>();
    const loopsByStartLine = new Map<number, LoopDetailsBuilder>();

    collectLineDetailsRecursive(results, steps, 0, () => false, {
        methodsByStartLine,
        loopsByStartLine
    });
    for (const method of methodsByStartLine.values()) {
        results.push(method);
    }

    console.log('steps', steps);
    console.log('results', results);
    return results;
};