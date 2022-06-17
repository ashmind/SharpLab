import type { FlowStep } from '../../../../shared/resultTypes';

export type FlowEventPart = {
    type: 'code' | 'jump-down' | 'jump-up' | 'notes' | 'exception';
    text: string;
};

export type FlowEvent = {
    parts: ReadonlyArray<FlowEventPart>;
};

const fillEmptyLines = (flow: ReadonlyArray<FlowStep>, lines: ReadonlyArray<string>) => {
    const results = [] as Array<FlowStep>;
    let previous: FlowStep | undefined;
    for (const step of flow) {
        if (previous && previous.line < step.line - 1) {
            const trivia = [];
            let filled = true;
            for (let line = previous.line + 1; line <= step.line - 1; line++) {
                if (!/^\s*$/.test(lines[line - 1])) {
                    filled = false;
                    break;
                }
                trivia.push({ line });
            }
            if (filled)
                results.push(...trivia);
        }
        results.push(step);
        previous = step;
    }

    return results as ReadonlyArray<FlowStep>;
};

const trimPunctuation = (line: string) => line.replace(/^\s*[{};]|[{};]\s*$/g, '');

const extractEventFromStep = (step: FlowStep, previous: FlowStep|undefined, lines: ReadonlyArray<string>) => {
    const parts = [] as Array<FlowEventPart>;
    if (previous && step.line !== previous.line + 1) {
        const sourceLine = trimPunctuation(lines[previous.line - 1]);
        const targetLine = trimPunctuation(lines[step.line - 1]);
        parts.push({
            type: step.line > previous.line ? 'jump-down' : 'jump-up',
            text: `${sourceLine} 🡆 ${targetLine}`
        });
    }

    if (step.notes) {
        /*if (parts.length === 0) {
            parts.push({
                type: 'code',
                text: trimPunctuation(lines[step.line - 1])
            });
        }*/
        parts.push({ type: 'notes', text: step.notes });
    }

    if (step.exception)
        parts.push({ type: 'exception', text: step.exception });

    return parts.length > 0 ? { parts } : null;
};

export const extractEvents = (flow: ReadonlyArray<FlowStep>, lines: ReadonlyArray<string>) => {
    const withEmptyLines = fillEmptyLines(flow, lines);
    return withEmptyLines
        .map((step, index) => extractEventFromStep(step, withEmptyLines[index - 1], lines))
        .filter(e => e) as ReadonlyArray<FlowEvent>;
};