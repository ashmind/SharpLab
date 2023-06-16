import { useEffect, useRef } from 'react';
import groupToMap from 'array.prototype.grouptomap';
import type { Flow, FlowArea } from '../../shared/resultTypes';
import { extractJumpsData, StepForJumps } from './internal/render/extractJumpsData';
import { processFlowIntoLineDetails } from './internal/render/processFlowIntoLineDetails';
import type { TrackerRoot } from './internal/render/trackingTypes';
import type { AreaVisitDetails, LineDetails, StepDetails } from './internal/render/detailsTypes';
import { renderNotesWidgets } from './internal/render/renderNotesWidgets';
import { renderAreaWidgets, setSelectedVisit } from './internal/render/renderAreaWidgets';

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

const renderAll = (cm: CodeMirror.Editor, root: TrackerRoot) => {
    const lines = getAllActiveLines(root);
    const steps = [] as Array<StepDetails>;
    const visits = [] as Array<AreaVisitDetails>;
    const visitCountsByArea = new Map<FlowArea, number>();

    collectStepsAndVisits({ steps, visits }, lines, visitCountsByArea);

    const visitDetailsByArea = groupToMap(visits, l => l.area);
    const stepDetailsByLine = groupToMap(steps, l => l.line);

    let rendering = true;
    let renderAllRequestedDuringRender = false;
    renderAreaWidgets(
        cm, visitDetailsByArea, root,
        () => {
            if (rendering) {
                renderAllRequestedDuringRender = true;
                return;
            }
            renderAll(cm, root);
        }
    );
    rendering = false;
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    if (renderAllRequestedDuringRender) {
        renderAll(cm, root);
        return;
    }

    renderNotesWidgets(cm, stepDetailsByLine, root);

    const jumps = extractJumpsData(
        root.details.jumps,
        getAllStepsForJumps(steps, visits, root)
    );
    cm.setJumpArrows(jumps);
};

const applyInitialSelectionIfAny = (
    cm: CodeMirror.Editor,
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
        renderAll(cm, root);
};

const renderExecutionFlow = (
    cm: CodeMirror.Editor,
    flow: Flow | null,
    root: TrackerRoot,
    initialSelectRule?: (area: FlowArea) => number | null
) => {
    root.details = processFlowIntoLineDetails(flow);
    renderAll(cm, root);
    applyInitialSelectionIfAny(cm, root, initialSelectRule);
};

export const useRenderExecutionFlow = (
    cm: CodeMirror.Editor | undefined,
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
        if (!cm)
            return;

        const tracker = trackerRef.current;
        renderExecutionFlow(cm, flow, tracker, initialSelectRule);
    }, [cm, flow, initialSelectRule]);
};