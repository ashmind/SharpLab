import Vue from 'vue';
import { AstNode } from 'app/results/ast/AstNode';

export default Vue.extend({
    props: {
        item: {}
    },
    template: `<react-ast-node v-bind:item="item" />`,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    components: { 'react-ast-node': AstNode as any }
});