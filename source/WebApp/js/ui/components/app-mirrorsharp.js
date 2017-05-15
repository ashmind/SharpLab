import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';
import 'codemirror/mode/mllike/mllike';

Vue.component('app-mirrorsharp', {
    props: {
        value:           String,
        serverOptions:   Object,
        serviceUrl:      String
    },
    mounted: function() {
        Vue.nextTick(() => {
            const textarea = this.$el;
            textarea.value = this.value;
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
            this.$watch('serverOptions', o => instance.sendServerOptions(o), { deep: true });
            this.$watch('serviceUrl', u => {
                instance.destroy({ keepCodeMirror: true });
                options.serviceUrl = u;
                instance = mirrorsharp(textarea, options);
                if (this.serverOptions)
                    instance.sendServerOptions(this.serverOptions);
            });
        });
    },
    template: '<textarea></textarea>'
});