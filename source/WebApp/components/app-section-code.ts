import Vue from 'vue';
import type { AppOptions } from 'ts/types/app';
import type { ParsedResult, Result } from 'ts/types/results';
import type { Branch } from 'ts/types/branch';
import type { Gist } from 'ts/types/gist';
import type { MirrorSharpConnectionState, MirrorSharpSlowUpdateResult } from 'mirrorsharp';
import type { ServerOptions } from 'ts/types/server-options';
import type { HighlightedRange } from 'ts/types/highlighted-range';
import { CodeTopSection } from 'app/CodeTopSection';

export default Vue.component('app-section-code', {
    props: {
        options: Object as () => AppOptions,
        branches: Array as () => ReadonlyArray<Branch>,
        result: Object as () => ParsedResult,

        initialCode: String,
        codeEditorProps: Object as () => {
            serverOptions: ServerOptions;
            highlightedRange: HighlightedRange | null;
        },

        gist: Object as () => Gist
    },
    computed: {
        fullCodeEditorProps() {
            return {
                ...this.codeEditorProps,
                onSlowUpdateWait: () => this.$emit('slow-update-wait'),
                onSlowUpdateResult: (result: MirrorSharpSlowUpdateResult<Result['value']>) => this.$emit('slow-update-result', result),
                onConnectionChange: (state: MirrorSharpConnectionState) => this.$emit('connection-change', state),
                onCursorMove: (getOffset: () => number) => this.$emit('cursor-move', getOffset),
                onServerError: (message: string) => this.$emit('server-error', message)
            };
        }
    },

    template: `<react-code-top-section
        class="temp-react-wrapper"
        v-bind:options="options"
        v-bind:branches="branches"
        v-bind:result="result"
        v-bind:initialCode="initialCode"
        v-bind:codeEditorProps="fullCodeEditorProps"
        v-bind:gist="gist"
    ></react-code-top-section>`,

    components: {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        'react-code-top-section': CodeTopSection as any
    }
});