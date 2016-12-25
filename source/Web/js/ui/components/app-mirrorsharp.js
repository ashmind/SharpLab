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
            afterSlowUpdate: result  => this.$emit('after-slow-update', result),
            afterTextChange: getText => this.$emit('after-text-change', getText),
            onServerError:   message => this.$emit('server-error', message)
        });
        if (this.serverOptions)
            instance.setServerOptions(this.serverOptions);
        this.$watch('serverOptions', o => instance.setServerOptions(o), { deep: true });
    },
    template: '<textarea></textarea>'
});