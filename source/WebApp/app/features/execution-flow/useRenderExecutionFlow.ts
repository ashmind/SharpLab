import { useEffect, useRef } from 'react';
import type { FlowStep } from '../../shared/resultTypes';
// import { extractJumps } from './internal/render/extractJumps';
// import type { FlowStepForJumps } from './internal/render/FlowStepForJumps';
import { type RepeatAreaDetails, type Visit, type LineDetails, processStepsIntoLineDetails } from './internal/render/processStepsIntoLineDetails';

type TrackingTree = {
    bookmarks: Array<CodeMirror.TextMarker>;
    lineWidgets: Array<CodeMirror.LineWidget>;
    /*stepsForJumps: Array<FlowStepForJumps | {
        fromChildIndex: number;
        ignoreForJumpsOut?: boolean;
    }>;*/
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
    updateSubtree: (visit: Visit, nextVisit?: Visit) => void
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
        const nextVisit = visits[visits.indexOf(visit) + 1];
        updateSubtree(visit, nextVisit);
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
    //stepsForJumps: [],
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

/*const getAllStepsForJumps = (tree: TrackingTree) => {
    const allSteps = [] as Array<FlowStepForJumps>;
    for (const step of tree.stepsForJumps) {
        if ('fromChildIndex' in step) {
            const child = tree.children[step.fromChildIndex];
            const childSteps = getAllStepsForJumps(child);
            if (step.ignoreForJumpsOut && childSteps.length > 0) {
                childSteps[childSteps.length - 1] = {
                    ...childSteps[childSteps.length - 1],
                    ignoreForJumpsOut: true
                };
            }
            allSteps.push(...childSteps);
            continue;
        }
        allSteps.push(step);
    }
    return allSteps;
};*/

type TreeRenderContext = {
    cm: CodeMirror.Editor;
    rerenderAllJumps: () => void;
};

const renderTree = (
    lineDetails: ReadonlyArray<LineDetails>,
    trackingTree: TrackingTree,
    context: TreeRenderContext/*,
    loop?: { start: FlowStepForJumps; end?: FlowStepForJumps | null }*/
) => {
    const { cm } = context;
    clearWidgets(cm, trackingTree);

    // trackingTree.stepsForJumps = [];
    // if (loop)
    //     trackingTree.stepsForJumps.push(loop.start);
    for (let i = 0; i < lineDetails.length; i++) {
        const details = lineDetails[i];
        const cmLineNumber = details.line - 1;
        const line = cm.getLine(cmLineNumber);
        if (!line)
            continue;

        if (details.type === 'method') {
            // const lastStepBeforeLoop = trackingTree.stepsForJumps[trackingTree.stepsForJumps.length - 1];
            const start = getLineStart(line);
            const lineStartCoords = cm.cursorCoords({ line: cmLineNumber, ch: start }, 'local');

            const trackingSubtree = createTrackingTree();
            trackingTree.children.push(trackingSubtree);

            //lastStepBeforeLoop.ignoreForJumpsOut = true;
            const widget = createRepeatVisitSelectorWidget(
                details, lineStartCoords.left,
                (visit/*, nextVisit*/) => {
                    //lastStepBeforeLoop.ignoreForJumpsOut = false;
                    //const startForJumps = { ...visit.start };
                    //const endForJumps: FlowStepForJumps | null = nextVisit
                    //    ? { ...nextVisit.start, ignoreForJumpsOut: true }
                    //    : null;
                    renderTree(
                        visit.lines ?? [], trackingSubtree, context/*,
                        { start: startForJumps, end: endForJumps }*/
                    );
                }
            );
            const lineWidget = cm.addLineWidget(cmLineNumber - 1, widget);
            trackingTree.lineWidgets.push(lineWidget);
            /*trackingTree.stepsForJumps.push({
                fromChildIndex: trackingTree.children.length - 1
            });*/

            continue;
        }

        const { step } = details;
        //trackingTree.stepsForJumps.push({ ...step });
        const end = line.length;
        for (const partName of ['notes', 'exception'] as const) {
            const part = step[partName];
            if (!part)
                continue;
            const widget = createNotesOrExceptionWidget(part, partName);
            trackingTree.bookmarks.push(cm.setBookmark({ line: cmLineNumber, ch: end }, { widget }));
        }
    }
    /*if (loop?.end)
        trackingTree.stepsForJumps.push(loop.end);*/

    context.rerenderAllJumps();
};

const renderExecutionFlow = (cm: CodeMirror.Editor, steps: ReadonlyArray<FlowStep|number>, tree: TrackingTree) => {
    const normalizedSteps = steps.map(step => typeof step === 'number' ? { line: step } : step);
    const lineDetails = processStepsIntoLineDetails(normalizedSteps);

    renderTree(lineDetails, tree, {
        cm,
        // eslint-disable-next-line @typescript-eslint/no-empty-function
        rerenderAllJumps: () => {}//cm.setJumpArrows(extractJumps(getAllStepsForJumps(tree)))
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