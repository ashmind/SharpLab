import Vue from 'vue';
import '../js/ui/directives/app-class-toggle.js';
import './app-diagnostic.js';

export default Vue.component('app-warnings-section', {
    props: {
        warnings: Array
    },
    template: '#app-warnings-section'
});