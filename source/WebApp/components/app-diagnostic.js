import Vue from 'vue';

Vue.component('app-diagnostic', {
    props: {
        model: Object
    },
    template: `<div class="diagnostic">
        <template v-if="model.id">{{model.severity}} {{model.id}}: </template>{{model.message}}
    </div>`.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});