import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';
import 'codemirror/mode/mllike/mllike';
import '../codemirror/addon-flow.js';
import groupToMap from '../../helpers/group-to-map.js';

Vue.component('app-mirrorsharp', {
    props: {
        initialText:      String,
        serverOptions:    Object,
        serviceUrl:       String,
        highlightedRange: Object,
        executionFlow:    Array
    },
    mounted: function() {
        Vue.nextTick(() => {
            const textarea = this.$el;
            textarea.value = this.initialText;
            const options = {
                serviceUrl: this.serviceUrl,
                on: {
                    slowUpdateResult: result => this.$emit('slow-update-result', result),
                    connectionChange: type => this.$emit('connection-change', type),
                    textChange: getText => this.$emit('text-change', getText),
                    serverError: message => this.$emit('server-error', message)
                }
            };
            let instance = mirrorsharp(textarea, options);
            if (this.serverOptions)
                instance.sendServerOptions(this.serverOptions);

            const contentEditable = instance
                .getCodeMirror()
                .getWrapperElement()
                .querySelector('[contentEditable=true]');
            if (contentEditable)
                contentEditable.setAttribute('autocomplete', 'off');

            this.$watch('initialText', v => instance.setText(v));
            this.$watch('serverOptions', o => instance.sendServerOptions(o), { deep: true });
            this.$watch('serviceUrl', u => {
                instance.destroy({ keepCodeMirror: true });
                options.serviceUrl = u;
                instance = mirrorsharp(textarea, options);
                if (this.serverOptions)
                    instance.sendServerOptions(this.serverOptions);
            });

            let currentMarker = null;
            this.$watch('highlightedRange', range => {
                const cm = instance.getCodeMirror();
                if (currentMarker) {
                    currentMarker.clear();
                    currentMarker = null;
                }
                if (!range)
                    return;

                const from = cm.posFromIndex(range.start);
                const to = cm.posFromIndex(range.end);
                currentMarker = cm.markText(from, to, { className: 'highlighted' });
            });

            const bookmarks = [];
            this.$watch('executionFlow', steps => {
                while (bookmarks.length > 0) {
                    bookmarks.pop().clear();
                }

                const cm = instance.getCodeMirror();
                cm.clearFlowPoints();
                if (!steps)
                    return;

                let lastLineNumber;
                let lastException;
                for (const step of steps) {
                    let lineNumber = step;
                    let exception = null;
                    if (typeof step === 'object') {
                        lineNumber = step.line;
                        exception = step.exception;
                    }

                    const important = (lastLineNumber != null && (lineNumber < lastLineNumber || lineNumber - lastLineNumber > 2)) || lastException;
                    if (important)
                        cm.addFlowJump(lastLineNumber - 1, lineNumber - 1, { throw: !!lastException });
                    lastLineNumber = lineNumber;
                    lastException = exception;
                }

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
            });
        });
    },
    template: '<textarea></textarea>'
});

function createFlowLineEndWidget(contents, kind) {
    const widget = document.createElement('span');
    widget.className = 'flow-line-end flow-line-end-' + kind;
    widget.textContent = contents.join('; ');
    return widget;
}