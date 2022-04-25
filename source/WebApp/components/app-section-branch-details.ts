import { BranchDetailsSection } from 'app/source/BranchDetailsSection';
import Vue from 'vue';
import type { Branch } from '../ts/types/branch';

export default Vue.component('app-section-branch-details', {
    props: {
        class: String,
        branch: Object as () => Branch|null
    },
    template: '<react-branch-details-section v-bind:className="class" v-bind:branch="branch"></react-branch-details-section>',
    components: {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        'react-branch-details-section': BranchDetailsSection as any
    }
});