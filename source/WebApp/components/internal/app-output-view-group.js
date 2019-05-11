import OutputViewSimple from './app-output-view-simple.js';

export default {
    name: 'app-output-view-group',
    props: {
        group: Object
    },
    template: '#app-output-view-group',
    components: {
        'sub-output-view-simple': OutputViewSimple
    }
};