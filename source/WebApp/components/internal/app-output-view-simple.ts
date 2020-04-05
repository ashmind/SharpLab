import Vue from 'vue';
import type { SimpleInspection } from '../../ts/types/results';

export default Vue.extend({
    props: {
        inspection: Object as () => SimpleInspection
    },
    computed: {
        // explicit return types are required due to:
        // https://github.com/vuejs/vue/issues/9873
        // https://github.com/microsoft/TypeScript/issues/30854

        isMultiline(): boolean {
            return !!this.inspection.value && /[\r\n]/.test(this.inspection.value);
        },

        isException(): boolean {
            return this.inspection.title === 'Exception';
        },

        isWarning(): boolean {
            return this.inspection.title === 'Warning';
        },

        isTitleOnly(): boolean {
            // eslint-disable-next-line no-undefined
            return this.inspection.value === undefined;
        }
    },
    template: '#app-output-view-simple'
});