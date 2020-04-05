import Vue from 'vue';
import OutputViewSimple from './app-output-view-simple';

export default Vue.extend({
    name: 'app-output-view-group',
    props: {
        group: Object
    },
    template: '#app-output-view-group',
    components: {
        'sub-output-view-simple': OutputViewSimple
    }
});