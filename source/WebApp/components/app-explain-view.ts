import Vue from 'vue';
import type { Explanation } from '../ts/types/results';
import '../ts/ui/mixins/markdown';

export default Vue.component('app-explain-view', {
    props: {
        explanations: Array as () => ReadonlyArray<Explanation>
    },
    methods: {
        a(string: string) {
            if (/\s.*ing$/.test(string))
                return '';
            return /^[aeiou]/.test(string) ? 'an' : 'a';
        }
    },
    template: '#app-explain-view'
});