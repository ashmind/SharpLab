import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';

Vue.component('app-mirrorsharp', {
    props: {
        value:           String/*,
        serviceUrl:      String,*/
    },
    ready: function() {
        const textarea = this.$el;
        textarea.value = this.value;
        mirrorsharp(textarea, {
            serviceUrl: "ws://" + window.location.host + "/mirrorsharp",
            afterSlowUpdate: result => this.$emit('after-slow-update', result),
            afterTextChange: getText => this.$emit('after-text-change', getText),
            onServerError:   message => this.$emit('server-error', message)
        });
    },
    template: '<textarea></textarea>'
});