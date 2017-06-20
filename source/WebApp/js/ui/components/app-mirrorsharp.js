import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';
import 'codemirror/mode/mllike/mllike';

Vue.component('app-mirrorsharp', {
    props: {
        initialText:      String,
        serverOptions:    Object,
        serviceUrl:       String,
        highlightedRange: Object
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
        });
    },
    template: '<textarea></textarea>'
});