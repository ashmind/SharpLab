import { useEffect, useRef } from 'react';
import type { FlowStep } from '../../shared/resultTypes';

const escapeNewLines = (text: string) => {
    return text
        .replace(/\r/g, '\\r')
        .replace(/\n/g, '\\n');
};

const groupStepsByLine = (steps: ReadonlyArray<FlowStep>) => {
    const map = new Map<number, Array<FlowStep>>();
    for (const step of steps) {
        let group = map.get(step.line);
        if (!group) {
            group = [];
            map.set(step.line, group);
        }
        group.push(step);
    }
    return map;
};

const createFlowLineEndWidget = (contents: ReadonlyArray<string>, kind: 'notes'|'exception') => {
    const widget = document.createElement('span');
    widget.className = 'flow-line-end flow-line-end-' + kind;
    widget.textContent = contents.map(escapeNewLines).join('; ');
    return widget;
};

const renderExecutionFlow = (steps: ReadonlyArray<FlowStep|number>, cm: CodeMirror.Editor) => {
    const jumpArrows = [];
    let lastLineNumber: number|undefined;
    let lastException: string|undefined|null;
    for (const step of steps) {
        if ((step as FlowStep).skipped)
            continue;
        let lineNumber = step as number;
        let exception = null;
        if (typeof step === 'object') {
            lineNumber = step.line;
            exception = step.exception;
        }

        const important = (lastLineNumber != null && (lineNumber < lastLineNumber || lineNumber - lastLineNumber > 2)) || lastException;
        if (important) {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            jumpArrows.push({ fromLine: lastLineNumber! - 1, toLine: lineNumber - 1, options: { throw: !!lastException } });
        }
        lastLineNumber = lineNumber;
        lastException = exception;
    }
    cm.setJumpArrows(jumpArrows);

    if (steps.length === 0)
        return { bookmarks: [] };

    const detailsByLine = groupStepsByLine(steps.filter(s => typeof s === 'object') as ReadonlyArray<FlowStep>);
    const bookmarks = [];
    for (const [lineNumber, details] of detailsByLine) {
        const cmLineNumber = lineNumber - 1;
        const line = cm.getLine(cmLineNumber);
        if (!line)
            continue;
        const end = line.length;
        for (const partName of ['notes', 'exception'] as const) {
            const parts = details.map(s => s[partName]).filter(p => p) as ReadonlyArray<string>;
            if (!parts.length)
                continue;
            const widget = createFlowLineEndWidget(parts, partName);
            bookmarks.push(cm.setBookmark({ line: cmLineNumber, ch: end }, { widget }));
        }
    }
    return { bookmarks };
};

export const useRenderExecutionFlow = (steps: ReadonlyArray<FlowStep|number> | null, cm: CodeMirror.Editor | undefined) => {
    const bookmarksRef = useRef<Array<CodeMirror.TextMarker>>([]);

    useEffect(() => {
        const clearBookmarks = () => {
            for (const bookmark of bookmarksRef.current) {
                bookmark.clear();
            }
        };
        clearBookmarks();
        if (!steps || !cm)
            return;
        const { bookmarks } = renderExecutionFlow(steps, cm);
        bookmarksRef.current = bookmarks;
        return clearBookmarks;
    }, [steps, cm]);
};