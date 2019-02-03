import Vue from 'vue';
import '../js/ui/mixins/markdown.js';

Vue.component('app-explain-view', {
    props: {
        explanations: Array
    },
    methods: {
        a(string) {
            if (/\s.*ing$/.test(string))
                return '';
            return /^[aeiou]/.test(string) ? 'an' : 'a';
        }
    },
    template: '#app-explain-view'
});