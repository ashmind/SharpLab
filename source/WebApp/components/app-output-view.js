import Vue from 'vue';
import OutputViewGroup from './internal/app-output-view-group.js';
import OutputViewSimple from './internal/app-output-view-simple.js';
import OutputViewMemory from './internal/app-output-view-memory.js';
import OutputViewGraph from './internal/app-output-view-graph.js';

export default Vue.component('app-output-view', {
    props: {
        output: Array
    },
    template: '#app-output-view',
    components: {
        'sub-output-view-group': OutputViewGroup,
        'sub-output-view-simple': OutputViewSimple,
        'sub-output-view-memory': OutputViewMemory,
        'sub-output-view-graph': OutputViewGraph
    }
});