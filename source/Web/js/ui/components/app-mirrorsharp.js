import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';

Vue.component('app-mirrorsharp', {
    props: {
        value:      String/*,
        serviceUrl: String*/
    },
    ready: function() {
        const textarea = this.$el;
        textarea.value = this.value;
        mirrorsharp(textarea, {
            serviceUrl: window.location.href.replace(/^http(.+)\/?$/i, 'ws$1/mirrorsharp')
        });
    },
    template: '<textarea></textarea>'
});