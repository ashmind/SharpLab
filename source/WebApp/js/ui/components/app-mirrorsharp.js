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
        executionFlow:     Object
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

                const visits = [];
                for (const number in lines) {
                    const data = lines[number];
                    if (data.visits) {
                        for (const visit of data.visits) {
                            visits.push({ visit, line: parseInt(number) });
                        }
                    }
                    else {
                        visits.push({ visit: data, line: parseInt(number) });
                    }

                    const noteWidget = createFlowLineNoteWidget(data);
                    if (noteWidget != null) {
                        const end = cm.getLine(number - 1).length;
                        const noteBookmark = cm.setBookmark({ line: number - 1, ch: end }, { widget: noteWidget });
                        bookmarks.push(noteBookmark);
                    }
                }

                visits.sort((a, b) => {
                    if (a.visit > b.visit) return  1;
                    if (a.visit < b.visit) return -1;
                    return 0;
                });

                for (let i = 1; i < visits.length; i++) {
                    const thisLine = visits[i].line;
                    const lastLine = visits[i-1].line;

                    if (thisLine >= lastLine && thisLine - lastLine < 3)
                        continue;

                    cm.addFlowJump(lastLine - 1, thisLine - 1);
                }
            });
        });
    },
    template: '<textarea></textarea>'
});

function createFlowLineNoteWidget(line) {
    if (!line.notes)
        return null;

    const widget = document.createElement('span');
    widget.className = 'flow-line-end-note';
    widget.textContent = line.notes;
    return widget;
}