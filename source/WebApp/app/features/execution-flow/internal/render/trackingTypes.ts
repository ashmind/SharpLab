import type { FlowArea } from '../../../../shared/resultTypes';
import type { AreaVisitDetails, FlowDetails } from './detailsTypes';

export type NotesTracker = {
    readonly element: HTMLSpanElement;
    readonly marker: CodeMirror.TextMarker;
};

export type AreaTracker = {
    readonly container: HTMLDivElement;
    readonly radioName: string;
    readonly widget: CodeMirror.LineWidget;

    readonly labelsByVisit: Map<AreaVisitDetails, HTMLLabelElement>;
    readonly visitsByInput: WeakMap<HTMLInputElement, AreaVisitDetails>;

    selectedVisit: AreaVisitDetails | null;
};

export type TrackerRoot = {
    currentDetails: FlowDetails;

    readonly notesMaps: {
        notes: Map<number, NotesTracker>;
        exception: Map<number, NotesTracker>;
    };
    readonly areaMap: Map<FlowArea, AreaTracker>;
};