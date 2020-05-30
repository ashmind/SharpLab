import Vue from 'vue';
import { targets, TargetName } from '../ts/helpers/targets';
import './app-select';

export default Vue.component('app-select-target', {
    props: {
        value: String as () => TargetName,
        useAriaLabel: {
            default: true,
            type: Boolean
        }
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

    inheritAttrs: false,

    template: '#app-select-target'
});