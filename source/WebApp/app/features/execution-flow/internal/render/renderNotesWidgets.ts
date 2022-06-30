import type { StepDetails } from './detailsTypes';
import { mergeToMap } from './mergeToMap';
import type { NotesTracker, TrackerRoot } from './trackingTypes';

const escapeNewLines = (text: string) => {
    return text
        .replace(/\r/g, '\\r')
        .replace(/\n/g, '\\n');
};

const setContent = (span: HTMLSpanElement, notes: ReadonlyArray<string>) => {
    span.textContent = escapeNewLines(notes.join(', '));
};

const createNotesOrExceptionWidget = (
    cm: CodeMirror.Editor,
    cmLine: string,
    line: number,
    notes: ReadonlyArray<string>,
    kind: 'notes'|'exception'
): NotesTracker => {
    const element = document.createElement('span');
    element.className = 'flow-line-end flow-line-end-' + kind;
    setContent(element, notes);

    const marker = cm.setBookmark({ line: line - 1, ch: cmLine.length }, { widget: element });
    return {
        element,
        marker
    };
};

const innerRenderNotesWidgets = (
    cm: CodeMirror.Editor,
    stepMap: ReadonlyMap<number, ReadonlyArray<StepDetails>>,
    root: TrackerRoot,
    type: 'notes'|'exception'
) => {
    const lineNotes = [...stepMap.entries()].map(([line, steps]) => ({
        line,
        notes: steps.map(s => s.step[type]).filter(n => n) as ReadonlyArray<string>,
        cmLine: cm.getLine(line - 1)
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    })).filter(n => n.cmLine != null && n.notes.length);

    mergeToMap(root.notesMaps[type], lineNotes, n => n.line, {
        create: ({ line, notes, cmLine }) => createNotesOrExceptionWidget(cm, cmLine, line, notes, type),
        update: ({ element }, { notes }) => setContent(element, notes),
        delete: ({ marker }) => marker.clear()
    });
};

export const renderNotesWidgets = (
    cm: CodeMirror.Editor,
    stepMap: ReadonlyMap<number, ReadonlyArray<StepDetails>>,
    root: TrackerRoot
) => {
    innerRenderNotesWidgets(cm, stepMap, root, 'notes');
    innerRenderNotesWidgets(cm, stepMap, root, 'exception');
};