import Vue from 'vue';

Vue.component('app-run-view', {
    props: {
        model: Object
    },
    template: `<div>{{model.returnValue}}</div>`
});