import Vue from 'vue';

Vue.component('app-branch-option', {
    props: {
        branch: Object,
        roslynVersion: String
    },
    template: `<option v-bind:value="branch">
        {{branch.name}} ({{branch.commits ? formatDate(branch.commits[0].date, 'd mmm yyyy') : roslynVersion}})
    </option>`.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});