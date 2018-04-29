import Vue from 'vue';

Vue.component('app-select', {
    props: {
        value: null
    },

    template: `<div class="select-wrapper">
      <select v-bind:value="value"
              v-on:change="$emit('input', $event.target.value)">
        <slot></slot>
      </select>
    </div>`.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});