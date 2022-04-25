import Vue from 'vue';
import { CodeEditor } from 'app/source/CodeEditor';
import type { LanguageName } from 'ts/helpers/languages';
import type { ServerOptions } from 'mirrorsharp/interfaces/protocol';
import type { HighlightedRange } from 'ts/types/highlighted-range';
import type { FlowStep, Result } from 'ts/types/results';
import type { MirrorSharpConnectionState, MirrorSharpSlowUpdateResult } from 'mirrorsharp';

export default Vue.component('app-code-edit', {
    props: {
        initialText:       String,
        initialCached:     Boolean,
        serviceUrl:        String,
        language:          String as () => LanguageName|undefined,
        serverOptions:     Object as () => ServerOptions|undefined,
        highlightedRange:  Object as () => HighlightedRange|undefined,
        executionFlow:     Array as () => Array<FlowStep>
    },
    methods: {
        onSlowUpdateWait() {
            this.$emit('slow-update-wait');
        },

        onSlowUpdateResult(result: MirrorSharpSlowUpdateResult<Result['value']>) {
            this.$emit('slow-update-result', result);
        },

        onConnectionChange(state: MirrorSharpConnectionState) {
            this.$emit('connection-change', state);
        },

        onTextChange(getText: () => string) {
            this.$emit('text-change', getText);
        },

        onCursorMove(getOffset: () => number) {
            this.$emit('cursor-move', getOffset);
        },

        onServerError(message: string) {
            this.$emit('server-error', message);
        }
    },
    template: `<react-code-editor
        class="temp-react-wrapper"
        v-bind:initialText="initialText"
        v-bind:initialCached="initialCached"
        v-bind:serviceUrl="serviceUrl"
        v-bind:language="language"
        v-bind:serverOptions="serverOptions"
        v-bind:highlightedRange="highlightedRange"
        v-bind:executionFlow="executionFlow"

        v-on:onSlowUpdateWait="onSlowUpdateWait"
        v-on:onSlowUpdateResult="onSlowUpdateResult"
        v-on:onConnectionChange="onConnectionChange"
        v-on:onTextChange="onTextChange"
        v-on:onCursorMove="onCursorMove"
        v-on:onServerError="onServerError"
    ></react-code-editor>`,
    components: {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        'react-code-editor': CodeEditor as any
    }
});