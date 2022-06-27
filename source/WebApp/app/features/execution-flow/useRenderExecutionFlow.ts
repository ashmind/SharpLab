import { useEffect, useRef } from 'react';
import type { Flow } from '../../shared/resultTypes';
import { extractJumpsData, StepForJumps } from './internal/render/extractJumpsData';
import { type AreaDetails, type AreaVisit, type LineDetails, processFlowIntoLineDetails } from './internal/render/processFlowIntoLineDetails';

type TrackingTree = {
    bookmarks: Array<CodeMirror.TextMarker>;
    lineWidgets: Array<CodeMirror.LineWidget>;
    stepsForJumps: Array<StepForJumps>;
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
    { visits }: AreaDetails,
    lineStartX: number,
    updateSubtree: (visit: AreaVisit) => void
) => {
    const widget = document.createElement('div');
    widget.className = 'flow-repeat-visit-selector';
    widget.style.paddingLeft = lineStartX + 'px';

    const visitByElement = new WeakMap<HTMLElement, AreaVisit>();
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
    stepsForJumps: [],
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

const getAllStepsForJumps = (tree: TrackingTree) => {
    const generator = function*(tree: TrackingTree): Generator<StepForJumps> {
        for (const step of tree.stepsForJumps) {
            yield step;
        }
        for (const child of tree.children) {
            yield* generator(child);
        }
    };

    return [...generator(tree)];
};

type TreeRenderContext = {
    cm: CodeMirror.Editor;
    renderAllJumps: () => void;
};

const renderWidgetTree = (
    lineDetails: ReadonlyArray<LineDetails>,
    trackingTree: TrackingTree,
    context: TreeRenderContext
) => {
    const { cm } = context;
    clearWidgets(cm, trackingTree);

    trackingTree.stepsForJumps = [];
    for (const details of lineDetails) {
        const cmLineNumber = details.line - 1;
        const line = cm.getLine(cmLineNumber);
        if (!line)
            continue;

        if (details.type === 'area') {
            const trackingSubtree = createTrackingTree();
            trackingTree.children.push(trackingSubtree);

            const start = getLineStart(line);
            const lineStartCoords = cm.cursorCoords({ line: cmLineNumber, ch: start }, 'local');

            const widget = createRepeatVisitSelectorWidget(
                details, lineStartCoords.left,
                visit => {
                    renderWidgetTree(visit.lines, trackingSubtree, context);
                    trackingSubtree.stepsForJumps.unshift({ step: visit.start });
                    context.renderAllJumps();
                }
            );
            const lineWidget = cm.addLineWidget(cmLineNumber - 1, widget);
            trackingTree.lineWidgets.push(lineWidget);
            trackingSubtree.stepsForJumps.push(...details.visits.map(v => ({
                step: v.start,
                mode: 'jump-to-only'
            } as const)));
            continue;
        }

        const { step } = details;
        trackingTree.stepsForJumps.push({ step });
        const end = line.length;
        for (const partName of ['notes', 'exception'] as const) {
            const part = step[partName];
            if (!part)
                continue;
            const widget = createNotesOrExceptionWidget(part, partName);
            trackingTree.bookmarks.push(cm.setBookmark({ line: cmLineNumber, ch: end }, { widget }));
        }
    }
};

const renderExecutionFlow = (cm: CodeMirror.Editor, flow: Flow | null, tree: TrackingTree) => {
    const result = processFlowIntoLineDetails(flow);

    const renderAllJumps = () => {
        const jumps = extractJumpsData(result.jumps, getAllStepsForJumps(tree));
        cm.setJumpArrows(jumps);
    };

    renderWidgetTree(result.lines, tree, { cm, renderAllJumps });
    renderAllJumps();
};

export const useRenderExecutionFlow = (cm: CodeMirror.Editor | undefined, flow: Flow | null) => {
    const trackingTreeRef = useRef<TrackingTree>(createTrackingTree());

    useEffect(() => {
        if (!cm)
            return;

        const tree = trackingTreeRef.current;
        renderExecutionFlow(cm, flow, tree);
        return () => {
            clearWidgets(cm, tree);
            cm.setJumpArrows([]);
        };
    }, [cm, flow]);
};