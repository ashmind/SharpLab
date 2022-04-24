import Vue from 'vue';
import { CodeView } from 'app/results/CodeView';
import type { LinkedRange } from 'app/results/code/LinkedRange';
import type { targets } from '../ts/helpers/targets';

type TargetLanguageName = typeof targets.csharp|typeof targets.vb|typeof targets.il|typeof targets.asm;

export default Vue.component('app-code-view', {
    props: {
        value:    String,
        language: String as () => TargetLanguageName,
        ranges:   Array as () => ReadonlyArray<LinkedRange>|undefined
    },

    methods: {
        selectRange(range: LinkedRange) {
            this.$emit('range-active', range);
        }
    },

    template: '<react-code-view class="temp-react-wrapper" v-bind:code="value" v-bind:language="language" v-bind:ranges="ranges" v-on:onRangeSelect="selectRange"></react-code-view>',

    components: {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        'react-code-view': CodeView as any
    }
});