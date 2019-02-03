import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';
import 'codemirror/mode/mllike/mllike';
import './internal/codemirror/addon-jump-arrows.js';
import groupToMap from '../js/helpers/group-to-map.js';

Vue.component('app-code-edit', {
    props: {
        initialText:       String,
        serviceUrl:        String,
        serverOptions:     Object,
        highlightedRange:  Object,
        executionFlow:     Array,
        compatibilityMode: Boolean
    },
    async mounted() {
        await Vue.nextTick();

        const textarea = this.$el;
        textarea.value = this.initialText;

        let instance;
        const options = {
            serviceUrl: this.serviceUrl,
            on: {
                slowUpdateWait: () => this.$emit('slow-update-wait'),
                slowUpdateResult: result => this.$emit('slow-update-result', result),
                connectionChange: type => this.$emit('connection-change', type),
                textChange: getText => this.$emit('text-change', getText),
                serverError: message => this.$emit('server-error', message)
            },
            sharplabPreQuickInfoCompatibilityMode: this.compatibilityMode
        };
        instance = mirrorsharp(textarea, options);
        if (this.serverOptions)
            instance.sendServerOptions(this.serverOptions);

        const cm = instance.getCodeMirror();

        const contentEditable = cm
            .getWrapperElement()
            .querySelector('[contentEditable=true]');
        if (contentEditable)
            contentEditable.setAttribute('autocomplete', 'off');

        this.$watch('initialText', v => instance.setText(v));
        this.$watch('serverOptions', o => instance.sendServerOptions(o), { deep: true });

        const recreate = () => {
            instance.destroy({ keepCodeMirror: true });
            instance = mirrorsharp(textarea, options);
            if (this.serverOptions)
                instance.sendServerOptions(this.serverOptions);
        };
        this.$watch('compatibilityMode', m => {
            options.sharplabPreQuickInfoCompatibilityMode = m;
            recreate();
        });
        this.$watch('serviceUrl', u => {
            options.serviceUrl = u;
            recreate();
        });

        let currentMarker = null;
        this.$watch('highlightedRange', range => {
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

        const bookmarks = [];
        this.$watch('executionFlow', steps => renderExecutionFlow(steps || [], instance.getCodeMirror(), bookmarks));

        const getCursorOffset = () => cm.indexFromPos(cm.getCursor());
        cm.on('cursorActivity', () => this.$emit('cursor-move', getCursorOffset));
    },
    template: '<textarea></textarea>'
});

function renderExecutionFlow(steps, cm, bookmarks) {
    while (bookmarks.length > 0) {
        bookmarks.pop().clear();
    }

    const jumpArrows = [];
    let lastLineNumber;
    let lastException;
    for (const step of steps) {
        if (step.skipped)
            continue;
        let lineNumber = step;
        let exception = null;
        if (typeof step === 'object') {
            lineNumber = step.line;
            exception = step.exception;
        }

        const important = (lastLineNumber != null && (lineNumber < lastLineNumber || lineNumber - lastLineNumber > 2)) || lastException;
        if (important)
            jumpArrows.push({ fromLine: lastLineNumber - 1, toLine: lineNumber - 1, options: { throw: !!lastException } });
        lastLineNumber = lineNumber;
        lastException = exception;
    }
    cm.setJumpArrows(jumpArrows);

    if (steps.length === 0)
        return;

    const detailsByLine = groupToMap(steps.filter(s => typeof s === 'object'), s => s.line);
    for (const [lineNumber, details] of detailsByLine) {
        const cmLineNumber = lineNumber - 1;
        const end = cm.getLine(cmLineNumber).length;
        for (const partName of ['notes', 'exception']) {
            const parts = details.map(s => s[partName]).filter(p => p);
            if (!parts.length)
                continue;
            const widget = createFlowLineEndWidget(parts, partName);
            bookmarks.push(cm.setBookmark({ line: cmLineNumber, ch: end }, { widget }));
        }
    }
}

function createFlowLineEndWidget(contents, kind) {
    const widget = document.createElement('span');
    widget.className = 'flow-line-end flow-line-end-' + kind;
    widget.textContent = contents.join('; ');
    return widget;
}