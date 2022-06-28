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
    visitLabel.className = 'flow-repeat-visit';
    visitLabel.textContent = visit.start.step.notes ?? '•';
    visitLabel.prepend(visitRadio);
    visitsByInput.set(visitRadio, visit);
    return visitLabel;
};

let visitSelectorNameIndex = 1;
const createAreaWidget = (
    cm: CodeMirror.Editor,
    visits: ReadonlyArray<AreaVisitDetails>,
    requestRenderAll: () => void
): AreaTracker => {
    const { area } = visits[0];

    const cmLine = cm.getLine(area.startLine - 1);

    const container = document.createElement('div');
    container.className = 'flow-repeat-visit-selector';
    container.style.paddingLeft = getLineStart(cmLine) + 'px';

    // eslint-disable-next-line no-plusplus
    const radioName = 'flow-repeat-visit-selector-' + visitSelectorNameIndex++;
    const visitsByInput = new WeakMap();
    const labelsByVisit = new Map();

    for (const visit of orderVisits(visits)) {
        const visitLabel = createAreaWidgetChoiceElement(visit, { radioName, visitsByInput });
        labelsByVisit.set(visit, visitLabel);
        container.appendChild(visitLabel);
    }

    let selectedLabel: HTMLLabelElement | undefined;
    // eslint-disable-next-line prefer-const
    let tracker: AreaTracker;
    container.addEventListener('change', e => {
        const visit = visitsByInput.get(e.target as HTMLInputElement);
        if (!visit)
            return;

        if (selectedLabel)
            selectedLabel.classList.remove('flow-repeat-visit-selected');

        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        selectedLabel = (e.target as HTMLElement).parentElement! as HTMLLabelElement;
        selectedLabel.classList.add('flow-repeat-visit-selected');
        tracker.selectedVisit = visit;
        requestRenderAll();
    });

    const widget = cm.addLineWidget(area.startLine - 1, container, { above: true });
    tracker = {
        container,
        radioName,

        visitsByInput,
        labelsByVisit,

        widget,

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

    // cheap way to ensure correct sort order
    for (const label of tracker.labelsByVisit.values()) {
        label.remove();
    }
    for (const visit of orderVisits(visits)) {
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
    mergeToMap(root.areaMap, [...visitMap.entries()], ([area]) => area, {
        create: ([, visits]) => createAreaWidget(cm, visits, requestRenderAll),
        update: (widget, [, visits]) => updateAreaWidget(widget, visits, requestRenderAll),
        delete: ({ widget }) => widget.clear()
    });
};