import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';

Vue.component('app-mirrorsharp', {
    props: {
        value:           String,
        serverOptions:   Object/*,
        serviceUrl:      String,*/
    },
    ready: function() {
        const textarea = this.$el;
        textarea.value = this.value;
        const instance = mirrorsharp(textarea, {
            serviceUrl: "ws://" + window.location.host + "/mirrorsharp",
            on: {
                slowUpdateResult: result => this.$emit('slow-update-result', result),
                connectionChange: type => this.$emit('connection-change', type),
                textChange: getText => this.$emit('text-change', getText),
                serverError: message => this.$emit('server-error', message)
            }
        });
        if (this.serverOptions)
            instance.sendServerOptions(this.serverOptions);
        this.$watch('serverOptions', o => instance.sendServerOptions(o), { deep: true });
    },
    template: '<textarea></textarea>'
});