import Vue from 'vue';

Vue.component('app-diagnostic', {
    props: {
        model: Object,
        severity: String
    },
    template: `<div class="diagnostic">
        <template v-if="model.start || model.end">({{model.start.line}},{{model.start.column}},{{model.end.line}},{{model.end.column}}): </template>
        <template v-if="model.id">{{severity}} {{model.id}}: </template>{{model.message}}
    </div>`.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});