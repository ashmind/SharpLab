import Vue from 'vue';
import type { LinkedCodeRange } from 'app/results/CodeView';
import type { AppOptions } from 'ts/types/app';
import type { AstItem, Result } from 'ts/types/results';
import { ResultsSection } from 'app/ResultsSection';
import { assertMatchesRef } from 'ts/helpers/assert-matches-ref';
import type { AstViewRef } from 'ts/types/component-ref-interfaces/ast-view-ref';

const AppResultsSection = Vue.component('app-results-section', {
    props: {
        options:  Object as () => AppOptions,
        result:   Object as () => Result
    },
    data: () => ({
        selectedCodeOffset: null as number | null
    }),
    methods: {
        selectCodeRange(range: LinkedCodeRange) {
            this.$emit('code-range-active', range);
        },
        selectAstItem(item: AstItem) {
            this.$emit('ast-item-select', item);
        },
        selectDeepestByOffset(offset: number) {
            this.selectedCodeOffset = offset;
        }
    },

    template: `<react-results-section
        class="temp-react-wrapper"
        v-bind:options="options"
        v-bind:result="result"
        v-bind:selectedCodeOffset="selectedCodeOffset"
        v-on:onAstSelect="selectAstItem"
        v-on:onCodeRangeSelect="selectCodeRange"></react-results-section>`,

    components: {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        'react-results-section': ResultsSection as any
    }
});

assertMatchesRef<AstViewRef, InstanceType<typeof AppResultsSection>>();

export default AppResultsSection;