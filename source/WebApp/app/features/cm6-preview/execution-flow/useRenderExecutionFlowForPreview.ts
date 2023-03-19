import { useEffect, useRef } from 'react';
import type { EditorView } from '@codemirror/view';
import type { Flow, FlowArea } from '../../../shared/resultTypes';
import { setJumpsEffect } from '../extensions/jumpArrows';
import { processFlowIntoLineDetails } from '../../execution-flow/internal/render/processFlowIntoLineDetails';
import type { TrackerRoot } from '../../execution-flow/internal/render/trackingTypes';
import type { AreaVisitDetails, LineDetails, StepDetails } from '../../execution-flow/internal/render/detailsTypes';
import { setSelectedVisit } from '../../execution-flow/internal/render/renderAreaWidgets';
import { extractJumpsData, StepForJumps } from './internal/render/extractJumpsData';

const getAllActiveLines = ({ details, areaMap }: TrackerRoot) => {
    return [
        ...details.lines,
        ...[...areaMap.values()].flatMap(
            ({ selectedVisit }) => selectedVisit?.lines ?? []
        )
    ];
};

const getAllStepsForJumps = (
    steps: ReadonlyArray<StepDetails>,
    visits: ReadonlyArray<AreaVisitDetails>,
    { areaMap }: TrackerRoot
): ReadonlyArray<StepForJumps> => {
    const getExtraStepsFromVisit = (visit: AreaVisitDetails): ReadonlyArray<StepForJumps> => {
        const selected = areaMap.get(visit.area)?.selectedVisit;
        if (selected && selected !== visit)
            return [];

        // 1. If this visit is selected then getAllActiveLines will almost handle it,
        // but we need to add the start line manually as "active lines" does not add it
        // to avoid creating a label where we already create a widget.
        // 2. If no visit is selected, then we will always return start of every visit,
        // but for jump in only.
        return [{
            step: visit.start.step,
            mode: selected ? 'any' : 'jump-to-only'
        }];
    };

    return [
        ...steps.map(l => ({ step: l.step })),
        ...visits.flatMap(getExtraStepsFromVisit)
    ];
};

const isRecursive = (visit: AreaVisitDetails, area?: FlowArea): boolean => {
    if (area && visit.area === area)
        return true;

    area ??= visit.area;
    return visit.lines
        .some(l => l.type === 'area' && isRecursive(l, area));
};

const collectStepsAndVisits = (
    results: {
        steps: Array<StepDetails>;
        visits: Array<AreaVisitDetails>;
    },
    lines: ReadonlyArray<LineDetails>,
    visitCountsByArea: Map<FlowArea, number>
) => {
    for (const line of lines) {
        if (line.type === 'area')
            visitCountsByArea.set(line.area, (visitCountsByArea.get(line.area) ?? 0) + 1);
    }

    for (const line of lines) {
        if (line.type === 'area') {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            if (visitCountsByArea.get(line.area)! === 1 && !isRecursive(line)) {
                visitCountsByArea.set(line.area, 0);
                results.steps.push(line.start);
                collectStepsAndVisits(results, line.lines, visitCountsByArea);
                continue;
            }

            results.visits.push(line);
            continue;
        }

        results.steps.push(line);
    }
};

const renderAll = (view: EditorView, root: TrackerRoot) => {
    const lines = getAllActiveLines(root);
    const steps = [] as Array<StepDetails>;
    const visits = [] as Array<AreaVisitDetails>;
    const visitCountsByArea = new Map<FlowArea, number>();

    collectStepsAndVisits({ steps, visits }, lines, visitCountsByArea);

    /*
    const visitDetailsByArea = groupToMap(visits, l => l.area);
    const stepDetailsByLine = groupToMap(steps, l => l.line);

    let rendering = true;
    let renderAllRequestedDuringRender = false;
    renderAreaWidgets(
        view, visitDetailsByArea, root,
        () => {
            if (rendering) {
                renderAllRequestedDuringRender = true;
                return;
            }
            renderAll(view, root);
        }
    );
    rendering = false;
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    if (renderAllRequestedDuringRender) {
        renderAll(view, root);
        return;
    }

    renderNotesWidgets(view, stepDetailsByLine, root);
    */
    const jumps = extractJumpsData(
        root.details.jumps,
        getAllStepsForJumps(steps, visits, root)
    );
    view.dispatch({ effects: setJumpsEffect.of(jumps) });
};

const applyInitialSelectionIfAny = (
    view: EditorView,
    root: TrackerRoot,
    rule?: (area: FlowArea) => number | null
) => {
    if (!rule)
        return;

    let selectionUpdated = false;
    for (const [area, tracker] of root.areaMap.entries()) {
        const selectedIndex = rule(area);
        if (selectedIndex == null)
            continue;

        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        setSelectedVisit(tracker, tracker.orderedVisits[selectedIndex]!);
        selectionUpdated = true;
    }

    if (selectionUpdated)
        renderAll(view, root);
};

const renderExecutionFlow = (
    view: EditorView,
    flow: Flow | null,
    root: TrackerRoot,
    initialSelectRule?: (area: FlowArea) => number | null
) => {
    root.details = processFlowIntoLineDetails(flow);
    renderAll(view, root);
    applyInitialSelectionIfAny(view, root, initialSelectRule);
};

export const useRenderExecutionFlowForPreview = (
    view: EditorView | null,
    flow: Flow | null,
    initialSelectRule?: (area: FlowArea) => number | null
) => {
    const trackerRef = useRef<TrackerRoot>({
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        details: null!,
        notesMaps: {
            notes: new Map(),
            exception: new Map()
        },
        areaMap: new Map()
    });

    useEffect(() => {
        if (!view)
            return;

        const tracker = trackerRef.current;
        renderExecutionFlow(view, flow, tracker, initialSelectRule);
    }, [view, flow, initialSelectRule]);
};