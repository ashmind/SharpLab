import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';
import 'codemirror/mode/mllike/mllike';
import '../codemirror/addon-flow.js';

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
            this.$watch('executionFlow', lines => {
                while (bookmarks.length > 0) {
                    bookmarks.pop().clear();
                }

                const cm = instance.getCodeMirror();
                cm.clearFlowPoints();
                if (!lines)
                    return;

                const notes = {};
                let lastLineNumber;
                for (const line of lines) {
                    let lineNumber = line;
                    if (typeof line === 'object') {
                        lineNumber = line.line;
                        (notes[lineNumber] || (notes[lineNumber] = [])).push(line.notes);
                    }

                    if (lastLineNumber != null && (lineNumber < lastLineNumber || lineNumber - lastLineNumber > 2))
                        cm.addFlowJump(lastLineNumber - 1, lineNumber - 1);
                    lastLineNumber = lineNumber;
                }

                for (const lineNumber in notes) {
                    const cmLineNumber = parseInt(lineNumber) - 1;
                    const noteWidget = createFlowLineNoteWidget(notes[lineNumber]);
                    const end = cm.getLine(cmLineNumber).length;
                    const noteBookmark = cm.setBookmark({ line: cmLineNumber, ch: end }, { widget: noteWidget });
                    bookmarks.push(noteBookmark);
                }
            });
        });
    },
    template: '<textarea></textarea>'
});

function createFlowLineNoteWidget(notes) {
    const widget = document.createElement('span');
    widget.className = 'flow-line-end-note';
    widget.textContent = notes.join('; ');
    return widget;
}