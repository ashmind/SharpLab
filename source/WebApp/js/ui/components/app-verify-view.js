import Vue from 'vue';

Vue.component('app-verify-view', {
    props: {
        value: String
    },
    template: `<div class="result-content">{{value}}</div>`
});