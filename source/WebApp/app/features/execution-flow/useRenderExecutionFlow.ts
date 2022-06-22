import { useEffect, useRef } from 'react';
import type { FlowStep } from '../../shared/resultTypes';
import { extractJumps } from './internal/render/extractJumps';
import { type RepeatAreaDetails, type Visit, type LineDetails, processStepsIntoLineDetails, SimpleLineDetails } from './internal/render/processStepsIntoLineDetails';

type ChildLineDetailsSource = {
    fromChildIndex: number;
};

type TrackingTree = {
    bookmarks: Array<CodeMirror.TextMarker>;
    lineWidgets: Array<CodeMirror.LineWidget>;
    linesForJumps: Array<SimpleLineDetails | ChildLineDetailsSource>;
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

let visitSelectorNameIndex = 1;
const createRepeatVisitSelectorWidget = (
    { visits }: RepeatAreaDetails,
    lineStartX: number,
    updateSubtree: (visit: Visit) => void
) => {
    const widget = document.createElement('div');
    widget.className = 'flow-repeat-visit-selector';
    widget.style.paddingLeft = lineStartX + 'px';

    const visitByElement = new WeakMap<HTMLElement, Visit>();
    // eslint-disable-next-line no-plusplus
    const radioName = 'flow-repeat-visit-selector-' + visitSelectorNameIndex++;

    for (const visit of visits) {
        const visitLabel = document.createElement('label');
        const visitRadio = document.createElement('input');
        visitRadio.type = 'radio';
        visitRadio.name = radioName;
        visitLabel.className = 'flow-repeat-visit';
        visitLabel.textContent = visit.start.notes ?? '•';
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
            selectedLabel.classList.remove('flow-repeat-visit-selected');

        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        selectedLabel = (e.target as HTMLElement).parentElement! as HTMLLabelElement;
        selectedLabel.classList.add('flow-repeat-visit-selected');
        updateSubtree(visit);
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
    lineWidgets: [],
    linesForJumps: [],
    children: []
});

const clearWidgets = (cm: CodeMirror.Editor, tree: TrackingTree) => {
    for (const bookmark of tree.bookmarks) {
        bookmark.clear();
    }
    tree.bookmarks = [];
    for (const lineWidget of tree.lineWidgets) {
        cm.removeLineWidget(lineWidget);
    }
    tree.lineWidgets = [];

    for (const child of tree.children) {
        clearWidgets(cm, child);
    }
    tree.children = [];
};

const getAllLinesForJumps = (tree: TrackingTree) => {
    const allLines = [] as Array<SimpleLineDetails>;
    for (const details of tree.linesForJumps) {
        if ('fromChildIndex' in details) {
            allLines.push(...getAllLinesForJumps(
                tree.children[details.fromChildIndex]
            ));
            continue;
        }
        allLines.push(details);
    }
    return allLines;
};

type TreeRenderContext = {
    cm: CodeMirror.Editor;
    rerenderAllJumps: () => void;
};

const renderTree = (
    lineDetails: ReadonlyArray<LineDetails>,
    trackingTree: TrackingTree,
    context: TreeRenderContext
) => {
    const { cm } = context;
    clearWidgets(cm, trackingTree);

    trackingTree.linesForJumps = [];
    for (const details of lineDetails) {
        const cmLineNumber = details.line - 1;
        const line = cm.getLine(cmLineNumber);
        if (!line)
            continue;

        if (details.type === 'method' || details.type === 'loop') {
            const trackingSubtree = createTrackingTree();
            trackingTree.children.push(trackingSubtree);
            trackingTree.linesForJumps.push({
                fromChildIndex: trackingTree.children.length - 1
            });

            if (details.visits.length === 1) {
                renderTree(details.visits[0].lines, trackingSubtree, context);
                continue;
            }

            const start = getLineStart(line);
            const lineStartCoords = cm.cursorCoords({ line: cmLineNumber, ch: start }, 'local');

            const widget = createRepeatVisitSelectorWidget(
                details, lineStartCoords.left,
                visit => renderTree(visit.lines, trackingSubtree, context)
            );
            const lineWidget = cm.addLineWidget(cmLineNumber - 1, widget);
            trackingTree.lineWidgets.push(lineWidget);
            continue;
        }

        const { step } = details;
        trackingTree.linesForJumps.push(details);
        const end = line.length;
        for (const partName of ['notes', 'exception'] as const) {
            const part = step[partName];
            if (!part)
                continue;
            const widget = createNotesOrExceptionWidget(part, partName);
            trackingTree.bookmarks.push(cm.setBookmark({ line: cmLineNumber, ch: end }, { widget }));
        }
    }

    context.rerenderAllJumps();
};

const renderExecutionFlow = (cm: CodeMirror.Editor, steps: ReadonlyArray<FlowStep|number>, tree: TrackingTree) => {
    const normalizedSteps = steps.map(step => typeof step === 'number' ? { line: step } : step);
    const lineDetails = processStepsIntoLineDetails(normalizedSteps);

    renderTree(lineDetails, tree, {
        cm,
        // eslint-disable-next-line @typescript-eslint/no-empty-function
        rerenderAllJumps: () => cm.setJumpArrows(extractJumps(getAllLinesForJumps(tree)))
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
            clearWidgets(cm, tree);
            cm.setJumpArrows([]);
        };
    }, [steps, cm]);
};