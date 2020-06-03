import Vue from 'vue';

export default Vue.component('app-select-mode', {
    props: {
        value: Boolean,
        useAriaLabel: {
            default: true,
            type: Boolean
        }
    },

    data() {
        return { release: this.value };
    },

    watch: {
        value() {
            this.release = this.value;
        },

        release() {
            this.$emit('input', this.release);
        }
    },

    inheritAttrs: false,

    template: `
        <app-select v-model="release"
                    class="option-optimizations option online-only"
                    v-bind:aria-label="useAriaLabel ? 'Build Mode' : null"
                    v-bind="$attrs">
            <option v-bind:value="false">Debug</option>
            <option v-bind:value="true">Release</option>
        </app-select>
    `
});