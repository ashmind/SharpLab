import Vue from 'vue';
import type { LinkedCodeRange } from 'app/results/CodeView';
import type { AppOptions } from 'ts/types/app';
import type { AstItem, Result } from 'ts/types/results';
import { assertMatchesRef } from 'ts/helpers/assert-matches-ref';
import type { AstViewRef } from 'ts/types/component-ref-interfaces/ast-view-ref';
import { ResultsTopSectionGroup } from 'app/ResultsTopSectionGroup';

const AppResultsSectionGroup = Vue.component('app-section-group-results', {
    props: {
        options:  Object as () => AppOptions,
        loading:  Boolean,
        result:   Object as () => Result
    },
    data: () => ({
        selectedCodeOffset: null as number | null
    }),
    methods: {
        onAstSelect(item: AstItem) {
            this.$emit('ast-item-select', item);
        },
        onCodeRangeSelect(range: LinkedCodeRange) {
            this.$emit('code-range-active', range);
        },
        selectDeepestByOffset(offset: number) {
            this.selectedCodeOffset = offset;
        }
    },

    template: `<react-result-top-section-group
        class="temp-react-wrapper"
        v-bind:options="options"
        v-bind:loading="loading"
        v-bind:result="result"
        v-bind:selectedCodeOffset="selectedCodeOffset"
        v-on:onAstSelect="onAstSelect"
        v-on:onCodeRangeSelect="onCodeRangeSelect"></react-result-top-section-group>`,

    components: {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        'react-result-top-section-group': ResultsTopSectionGroup as any
    }
});

assertMatchesRef<AstViewRef, InstanceType<typeof AppResultsSectionGroup>>();

export default AppResultsSectionGroup;