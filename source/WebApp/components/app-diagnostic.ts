import Vue from 'vue';
import type { Diagnostic, ServerError } from '../ts/types/results';

Vue.component('app-diagnostic', {
    props: {
        model: Object as () => Diagnostic|ServerError
    },
    template: `<div class="diagnostic">
        <template v-if="model.id">{{model.severity}} {{model.id}}: </template>{{model.message}}
    </div>`.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
});