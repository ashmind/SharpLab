import Vue from 'vue';
import { VerifyView } from '../app/results/VerifyView';

export default Vue.component('app-verify-view', {
    props: {
        value: String
    },
    template: `<react-verify-view v-bind:message="value" />`,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    components: { 'react-verify-view': VerifyView as any }
});