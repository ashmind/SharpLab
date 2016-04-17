import Vue from 'vue';

Vue.component('app-diagnostic', {
    props: {
        model: Object
    },
    template: `<div>
        ({{model.start.line}},{{model.start.column}},{{model.end.line}},{{model.end.column}}):
        {{model.severity}} {{model.id}}: {{model.message}}
    </div>`.replace(/\s{2,}/, ' ')
});