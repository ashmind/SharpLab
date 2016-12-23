import Vue from 'vue';
import mirrorsharp from 'mirrorsharp';

Vue.component('app-mirrorsharp', {
    props: {
        value:           String,/*,
        serviceUrl:      String,*/
        afterSlowUpdate: Function
    },
    ready: function() {
        const textarea = this.$el;
        textarea.value = this.value;

        let lastPendingSlowUpdateResult;
        this.$watch('afterSlowUpdate', value => {
            if (value && lastPendingSlowUpdateResult)
                value(lastPendingSlowUpdateResult);
            lastPendingSlowUpdateResult = null;
        });
        mirrorsharp(textarea, {
            serviceUrl: window.location.href.replace(/^http(.+)\/?$/i, 'ws$1/mirrorsharp'),
            afterSlowUpdate: result => {
                if (!this.afterSlowUpdate) {
                    lastPendingSlowUpdateResult = result;
                    return;
                }
                this.afterSlowUpdate(result);
                lastPendingSlowUpdateResult = null;
            }
        });
    },
    template: '<textarea></textarea>'
});