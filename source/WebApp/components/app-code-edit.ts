import Vue from 'vue';
import mirrorsharp, { MirrorSharpOptions } from 'mirrorsharp';
import 'codemirror/mode/mllike/mllike';
import type { FlowStep, Result } from '../ts/types/results';
import type { HighlightedRange } from '../ts/types/highlighted-range';
import type { MirrorSharpConnectionState } from '../ts/types/mirrorsharp';
import type { ServerOptions } from '../ts/types/server-options';
import './internal/codemirror/addon-jump-arrows';
import groupToMap from '../ts/helpers/group-to-map';

const AppCodeEdit = Vue.component('app-code-edit', {
    props: {
        initialText:       String,
        serviceUrl:        String,
        serverOptions:     Object as () => ServerOptions|undefined,
        highlightedRange:  Object as () => HighlightedRange|undefined,
        executionFlow:     Array as () => Array<FlowStep>
    },
    async mounted() {
        await Vue.nextTick();

        const textarea = this.$el as HTMLTextAreaElement;
        textarea.value = this.initialText;

        const options = {
            serviceUrl: this.serviceUrl,
            on: {
                slowUpdateWait: () => this.$emit('slow-update-wait'),
                slowUpdateResult: result => this.$emit('slow-update-result', result),
                connectionChange: (type: MirrorSharpConnectionState) => this.$emit('connection-change', type),
                textChange: getText => this.$emit('text-change', getText),
                serverError: message => this.$emit('server-error', message)
            }
        } as MirrorSharpOptions<Result['value']>;
        // incorrect, based on type _name_ match?
        // eslint-disable-next-line @typescript-eslint/no-unnecessary-type-arguments
        let instance = mirrorsharp<ServerOptions>(textarea, options);
        if (this.serverOptions)
            await instance.sendServerOptions(this.serverOptions);

        const cm = instance.getCodeMirror();

        const contentEditable = cm
            .getWrapperElement()
            .querySelector('[contentEditable=true]');
        if (contentEditable)
            contentEditable.setAttribute('autocomplete', 'off');

        this.$watch('initialText', (v: string) => instance.setText(v));
        this.$watch('serverOptions', (o: ServerOptions) => {
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            instance.sendServerOptions(o);
        }, { deep: true });

        const recreate = () => {
            instance.destroy({ keepCodeMirror: true });
            instance = mirrorsharp(textarea, options);
            if (this.serverOptions) {
                // eslint-disable-next-line @typescript-eslint/no-floating-promises
                instance.sendServerOptions(this.serverOptions);
            }
        };
        this.$watch('serviceUrl', (u: string) => {
            options.serviceUrl = u;
            recreate();
        });

        let currentMarker: CodeMirror.TextMarker|null = null;
        this.$watch('highlightedRange', (range: HighlightedRange|undefined) => {
            if (currentMarker) {
                currentMarker.clear();
                currentMarker = null;
            }
            if (!range)
                return;

            const from = typeof range.start === 'object' ? range.start : cm.posFromIndex(range.start);
            const to   = typeof range.end === 'object'   ? range.end : cm.posFromIndex(range.end);
            currentMarker = cm.markText(from, to, { className: 'highlighted' });
        });

        const bookmarks = [] as Array<CodeMirror.TextMarker>;
        this.$watch('executionFlow', (steps: ReadonlyArray<FlowStep|number>|undefined) => renderExecutionFlow(steps ?? [], instance.getCodeMirror(), bookmarks));

        const getCursorOffset = () => cm.indexFromPos(cm.getCursor());
        cm.on('cursorActivity', () => this.$emit('cursor-move', getCursorOffset));
    },
    template: '<textarea></textarea>'
});

function renderExecutionFlow(steps: ReadonlyArray<FlowStep|number>, cm: CodeMirror.Editor, bookmarks: Array<CodeMirror.TextMarker>) {
    while (bookmarks.length > 0) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        bookmarks.pop()!.clear();
    }

    const jumpArrows = [];
    let lastLineNumber: number|undefined;
    let lastException: string|undefined|null;
    for (const step of steps) {
        if ((step as FlowStep).skipped)
            continue;
        let lineNumber = step as number;
        let exception = null;
        if (typeof step === 'object') {
            lineNumber = step.line;
            exception = step.exception;
        }

        const important = (lastLineNumber != null && (lineNumber < lastLineNumber || lineNumber - lastLineNumber > 2)) || lastException;
        if (important) {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            jumpArrows.push({ fromLine: lastLineNumber! - 1, toLine: lineNumber - 1, options: { throw: !!lastException } });
        }
        lastLineNumber = lineNumber;
        lastException = exception;
    }
    cm.setJumpArrows(jumpArrows);

    if (steps.length === 0)
        return;

    const detailsByLine = groupToMap(steps.filter(s => typeof s === 'object') as ReadonlyArray<FlowStep>, s => s.line);
    for (const [lineNumber, details] of detailsByLine) {
        const cmLineNumber = lineNumber - 1;
        const end = cm.getLine(cmLineNumber).length;
        for (const partName of ['notes', 'exception'] as const) {
            const parts = details.map(s => s[partName]).filter(p => p) as ReadonlyArray<string>;
            if (!parts.length)
                continue;
            const widget = createFlowLineEndWidget(parts, partName);
            bookmarks.push(cm.setBookmark({ line: cmLineNumber, ch: end }, { widget }));
        }
    }
}

function createFlowLineEndWidget(contents: ReadonlyArray<string>, kind: 'notes'|'exception') {
    const widget = document.createElement('span');
    widget.className = 'flow-line-end flow-line-end-' + kind;
    widget.textContent = contents.map(escapeNewLines).join('; ');
    return widget;
}

function escapeNewLines(text: string) {
    return text
        .replace(/\r/g, '\\r')
        .replace(/\n/g, '\\n');
}

export default AppCodeEdit;