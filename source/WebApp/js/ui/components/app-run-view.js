import Vue from 'vue';

Vue.component('app-run-view', {
    props: {
        output: Array
    },
    template: `<div class="output">
      <small class="output-disclaimer">
        This is a new feature — might be unstable/too strict. Please <a href="https://github.com/ashmind/SharpLab/issues">report</a> any issues.
      </small>
      <template v-for="item in output">
        <pre v-if="typeof item === 'string'">{{item}}</pre>
        <div v-if="typeof item === 'object' && item.type === 'inspection' && item.value" class="inspection inspection-value-only">
          <h3>{{item.title}}:</h3>
          <div class="inspection-value">{{item.value}}</div>
        </div>
      </template>
    </div>`.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});