import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';

Vue.component('app-mirrorsharp', {
    props: {
        value:           String,/*,
        serviceUrl:      String,*/
        afterSlowUpdate: Function,
        onServerError:   Function
    },
    ready: function() {
        const textarea = this.$el;
        textarea.value = this.value;
        mirrorsharp(textarea, {
            serviceUrl: window.location.href.replace(/^http(.+)\/?$/i, 'ws$1/mirrorsharp'),
            afterSlowUpdate: result => this.$emit('after-slow-update', result),
            onServerError:   message => this.$emit('server-error', message)
        });
    },
    template: '<textarea></textarea>'
});