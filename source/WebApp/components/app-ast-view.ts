import Vue from 'vue';
import { AstView as ReactAstView } from 'app/results/AstView';
import { assertMatchesRef } from 'ts/helpers/assert-matches-ref';
import type { AstViewRef } from 'ts/types/component-ref-interfaces/ast-view-ref';
import type { AstItem } from '../ts/types/results';

const AstView = Vue.component('app-ast-view', {
    props: {
        roots: Array as () => ReadonlyArray<AstItem>
    },
    data: () => ({
        selectedOffset: null as number | null
    }),
    methods: {
        select(item: AstItem) {
            this.$emit('item-select', item);
        },

        selectDeepestByOffset(offset: number) {
            this.selectedOffset = offset;
        }
    },
    template: `<react-ast-view v-bind:roots="roots" v-bind:selectedOffset="selectedOffset" v-on:onSelect="select" />`,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    components: { 'react-ast-view': ReactAstView as any }
});

assertMatchesRef<AstViewRef, InstanceType<typeof AstView>>();

export default AstView;