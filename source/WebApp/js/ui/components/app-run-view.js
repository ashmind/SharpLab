import Vue from 'vue';

Vue.component('app-run-view', {
    props: {
        output: Array
    },
    template: `<div class="output">
      <div v-for="item in output">
        <div v-if="typeof item === 'string'">{{item}}</div>
        <div v-if="typeof item === 'object' && item.type === 'inspection' && item.value" class="inspection inspection-value-only">
          <h3>{{item.title}}:</h3>
          <div class="inspection-value">{{item.value}}</div>
        </div>
      </div>
    </div>`.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});