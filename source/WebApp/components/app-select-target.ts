import Vue from 'vue';
import { targets, TargetName } from '../ts/helpers/targets';
import './app-select';

export default Vue.component('app-select-target', {
    props: {
        id: String as () => string|null,
        value: String as () => TargetName
    },

    data() {
        return {
            target: this.value,
            targets
        };
    },

    watch: {
        value() {
            this.target = this.value;
        },

        target() {
            this.$emit('input', this.target);
        }
    },

    template: '#app-select-target'
});