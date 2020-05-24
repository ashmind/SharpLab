import Vue from 'vue';

export default Vue.component('app-select', {
    props: {
        id: String as () => string|null,
        value: null as unknown as () => unknown
    },

    data() {
        return { innerValue: this.value };
    },

    watch: {
        value() {
            this.innerValue = this.value;
        },

        innerValue() {
            this.$emit('input', this.innerValue);
        }
    },

    template: `<div class="select-wrapper">
      <select v-bind:id="id" v-model="innerValue">
        <slot></slot>
      </select>
    </div>`.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});