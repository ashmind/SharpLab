import Vue from 'vue';

export default Vue.component('app-verify-view', {
    props: {
        value: String
    },
    template: `<div class="result-content">{{value}}</div>`
});