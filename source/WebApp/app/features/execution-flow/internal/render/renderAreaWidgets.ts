import type { FlowArea } from '../../../../shared/resultTypes';
import type { AreaVisitDetails } from './detailsTypes';
import { mergeToMap } from './mergeToMap';
import type { AreaTracker, TrackerRoot } from './trackingTypes';

const getLineStart = (line: string) => {
    const match = /[^\s]/.exec(line);
    return match ? match.index : 0;
};

const orderVisits = (visits: ReadonlyArray<AreaVisitDetails>): ReadonlyArray<AreaVisitDetails> =>
    [...visits].sort((a, b) => a.order - b.order);

const createAreaWidgetChoiceElement = (
    visit: AreaVisitDetails,
    { radioName, visitsByInput }: Pick<AreaTracker, 'radioName'|'visitsByInput'>
) => {
    const visitLabel = document.createElement('label');
    const visitRadio = document.createElement('input');
    visitRadio.type = 'radio';
    visitRadio.name = radioName;
    visitLabel.className = 'flow-visit';
    visitLabel.textContent = visit.start.step.notes ?? '•';
    visitLabel.prepend(visitRadio);
    visitsByInput.set(visitRadio, visit);
    return visitLabel;
};

export const setSelectedVisit = (tracker: AreaTracker, visit: AreaVisitDetails | null) => {
    if (tracker.selectedVisit) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        tracker.labelsByVisit.get(tracker.selectedVisit)!.classList.remove('flow-visit-selected');
    }

    if (visit) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        tracker.labelsByVisit.get(visit)!.classList.add('flow-visit-selected');
    }
    tracker.selectedVisit = visit;
};

let visitSelectorNameIndex = 1;
const createAreaWidget = (
    cm: CodeMirror.Editor,
    cmLine: string,
    visits: ReadonlyArray<AreaVisitDetails>,
    requestRenderAll: () => void
): AreaTracker => {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const { area } = visits[0]!;

    const container = document.createElement('div');
    container.className = 'flow-visit-selector';
    container.style.paddingLeft = getLineStart(cmLine) + 'px';

    // eslint-disable-next-line no-plusplus
    const radioName = 'flow-visit-selector-' + visitSelectorNameIndex++;
    const visitsByInput = new WeakMap();
    const labelsByVisit = new Map();

    const orderedVisits = orderVisits(visits);

    for (const visit of orderedVisits) {
        const visitLabel = createAreaWidgetChoiceElement(visit, { radioName, visitsByInput });
        labelsByVisit.set(visit, visitLabel);
        container.appendChild(visitLabel);
    }

    // eslint-disable-next-line prefer-const
    let tracker: AreaTracker;
    let checkedInput = null as HTMLInputElement | null;
    container.addEventListener('change', e => {
        const input = e.target as HTMLInputElement;
        const visit = visitsByInput.get(input);
        if (!visit)
            return;

        setSelectedVisit(tracker, visit);
        requestRenderAll();
        setTimeout(() => checkedInput = input, 0);
    });

    container.addEventListener('click', e => {
        if (e.target !== checkedInput)
            return;

        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        checkedInput!.checked = false;
        setSelectedVisit(tracker, null);
        requestRenderAll();
        checkedInput = null;
    });

    const widget = cm.addLineWidget(area.startLine - 1, container, { above: true });
    tracker = {
        container,
        radioName,

        visitsByInput,
        labelsByVisit,

        widget,

        orderedVisits,
        selectedVisit: null
    };
    return tracker;
};

const updateAreaWidget = (
    tracker: AreaTracker,
    visits: ReadonlyArray<AreaVisitDetails>,
    requestRenderAll: () => void
) => {
    let changed = false;
    mergeToMap(tracker.labelsByVisit, visits, v => v, {
        create: visit => {
            changed = true;
            return createAreaWidgetChoiceElement(visit, tracker);
        },
        delete: (element, visit) => {
            element.remove();
            if (tracker.selectedVisit === visit) {
                tracker.selectedVisit = null;
                // impacts of selection change can only be
                // fully applied with a rerender
                requestRenderAll();
            }
            changed = true;
        }
    });
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    if (!changed)
        return;

    const orderedVisits = orderVisits(visits);
    tracker.orderedVisits = orderedVisits;

    // cheap way to ensure correct sort order
    for (const label of tracker.labelsByVisit.values()) {
        label.remove();
    }
    for (const visit of orderedVisits) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        tracker.container.appendChild(tracker.labelsByVisit.get(visit)!);
    }
};

export const renderAreaWidgets = (
    cm: CodeMirror.Editor,
    visitMap: ReadonlyMap<FlowArea, ReadonlyArray<AreaVisitDetails>>,
    root: TrackerRoot,
    requestRenderAll: () => void
) => {
    const data = [...visitMap.entries()].map(([area, visits]) => ({
        area,
        visits,
        cmLine: cm.getLine(area.startLine - 1)
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    })).filter(x => x.cmLine != null);

    mergeToMap(root.areaMap, data, x => x.area, {
        create: ({ visits, cmLine }) => createAreaWidget(cm, cmLine, visits, requestRenderAll),
        update: (widget, { visits }) => updateAreaWidget(widget, visits, requestRenderAll),
        delete: ({ widget }) => widget.clear()
    });
};