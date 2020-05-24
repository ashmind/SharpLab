import Vue from 'vue';
import type { Branch } from '../ts/types/branch';
import '../ts/ui/directives/app-class-toggle';

export default Vue.component('app-section-branch-details', {
    props: {
        branch: Object as () => Branch|null,
        header: {
            type: Boolean,
            default: true
        }
    },
    template: '#app-section-branch-details'
});