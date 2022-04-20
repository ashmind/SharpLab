import Vue from 'vue';
import { VerifyView } from '../app/results/VerifyView';

export default Vue.component('app-verify-view', {
    props: {
        value: String
    },
    template: `<react-verify-view v-bind:message="value" />`,
    components: { 'react-verify-view': VerifyView }
});