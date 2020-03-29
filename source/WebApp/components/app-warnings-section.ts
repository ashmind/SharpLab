import Vue from 'vue';
import type { DiagnosticWarning } from '../ts/types/results';
import '../ts/ui/directives/app-class-toggle';
import './app-diagnostic';

export default Vue.component('app-warnings-section', {
    props: {
        warnings: Array as () => ReadonlyArray<DiagnosticWarning>
    },
    template: '#app-warnings-section'
});