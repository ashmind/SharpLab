import { useEffect, useRef } from 'react';
import type { FlowStep } from '../../../shared/resultTypes';
import { extractJumps } from './extractJumps';
import { LineDetails, LoopDetails, LoopVisit, processStepsIntoLineDetails } from './processStepsIntoLineDetails';

type TrackingTree = {
    bookmarks: Array<CodeMirror.TextMarker>;
    jumps: Array<JumpData>;
    children: Array<TrackingTree>;
};

const escapeNewLines = (text: string) => {
    return text
        .replace(/\r/g, '\\r')
        .replace(/\n/g, '\\n');
};

const getLineStart = (line: string) => {
    const match = /[^\s]/.exec(line);
    return match ? match.index : 0;
};

let loopNameIndex = 1;
const createLoopWidget = (
    { visits }: LoopDetails,
    lineStartX: number,
    updateSubtree: (lineDetails: ReadonlyArray<LineDetails>) => void
) => {
    const widget = document.createElement('div');
    widget.className = 'flow-loop';
    widget.style.paddingLeft = lineStartX + 'px';

    const visitByElement = new WeakMap<HTMLElement, LoopVisit>();
    // eslint-disable-next-line no-plusplus
    const radioName = 'execution-flow-loop-' + loopNameIndex++;

    for (const visit of visits) {
        const visitLabel = document.createElement('label');
        const visitRadio = document.createElement('input');
        visitRadio.type = 'radio';
        visitRadio.name = radioName;
        visitLabel.className = 'flow-loop-visit';
        visitLabel.textContent = visit.start.notes ?? '?';
        visitLabel.prepend(visitRadio);
        visitByElement.set(visitRadio, visit);
        widget.appendChild(visitLabel);
    }

    let selectedLabel: HTMLLabelElement | undefined;
    widget.addEventListener('change', e => {
        const visit = visitByElement.get(e.target as HTMLElement);
        if (!visit)
            return;

        if (selectedLabel)
            selectedLabel.classList.remove('flow-loop-visit-selected');

        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        selectedLabel = (e.target as HTMLElement).parentElement! as HTMLLabelElement;
        selectedLabel.classList.add('flow-loop-visit-selected');
        updateSubtree(visit.lines ?? []);
    });

    return widget;
};

const createNotesOrExceptionWidget = (content: string, kind: 'notes'|'exception') => {
    const widget = document.createElement('span');
    widget.className = 'flow-line-end flow-line-end-' + kind;
    widget.textContent = escapeNewLines(content);
    return widget;
};

const createTrackingTree = (): TrackingTree => ({
    bookmarks: [],
    jumps: [],
    children: []
});

const clearWidgets = (tree: TrackingTree) => {
    for (const bookmark of tree.bookmarks) {
        bookmark.clear();
    }
    tree.bookmarks = [];
    for (const child of tree.children) {
        clearWidgets(child);
    }
    tree.children = [];
};

const collectAllJumps = (allJumps: Array<JumpData>, tree: TrackingTree) => {
    allJumps.push(...tree.jumps);
    for (const child of tree.children) {
        collectAllJumps(allJumps, child);
    }
};

const renderTree = (
    cm: CodeMirror.Editor,
    lineDetails: ReadonlyArray<LineDetails>,
    trackingTree: TrackingTree,
    rerenderAllJumps: () => void
) => {
    clearWidgets(trackingTree);

    let stepsToConsiderForJumps = [] as Array<FlowStep>;
    const jumps = [];
    for (let i = 0; i < lineDetails.length; i++) {
        const details = lineDetails[i];
        const cmLineNumber = details.line - 1;
        const line = cm.getLine(cmLineNumber);
        if (!line)
            continue;

        if (details.type === 'loop') {
            jumps.push(...extractJumps(stepsToConsiderForJumps));
            stepsToConsiderForJumps = [];

            const start = getLineStart(line);
            const lineStartCoords = cm.cursorCoords({ line: cmLineNumber, ch: start }, 'local');
            const trackingSubtree = createTrackingTree();
            trackingTree.children.push(trackingSubtree);
            const widget = createLoopWidget(
                details, lineStartCoords.left,
                lines => renderTree(cm, lines, trackingSubtree, rerenderAllJumps)
            );
            cm.addLineWidget(cmLineNumber - 1, widget);
            continue;
        }

        const { step } = details;
        stepsToConsiderForJumps.push(step);
        const end = line.length;
        for (const partName of ['notes', 'exception'] as const) {
            const part = step[partName];
            if (!part)
                continue;
            const widget = createNotesOrExceptionWidget(part, partName);
            trackingTree.bookmarks.push(cm.setBookmark({ line: cmLineNumber, ch: end }, { widget }));
        }
    }

    jumps.push(...extractJumps(stepsToConsiderForJumps));
    trackingTree.jumps = jumps;
    rerenderAllJumps();
};

const renderExecutionFlow = (cm: CodeMirror.Editor, steps: ReadonlyArray<FlowStep|number>, tree: TrackingTree) => {
    const normalizedSteps = steps.map(step => typeof step === 'number' ? { line: step } : step);
    const lineDetails = processStepsIntoLineDetails(normalizedSteps);

    renderTree(cm, lineDetails, tree, () => {
        const allJumps = [] as Array<JumpData>;
        collectAllJumps(allJumps, tree);
        cm.setJumpArrows(allJumps);
    });
};

export const useRenderExecutionFlow = (cm: CodeMirror.Editor | undefined, steps: ReadonlyArray<FlowStep|number> | null) => {
    const trackingTreeRef = useRef<TrackingTree>(createTrackingTree());

    useEffect(() => {
        if (!steps || !cm)
            return;

        const tree = trackingTreeRef.current;
        renderExecutionFlow(cm, steps, tree);
        return () => {
            clearWidgets(tree);
            cm.setJumpArrows([]);
        };
    }, [steps, cm]);
};