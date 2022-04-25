import Vue from 'vue';
import type { MirrorSharpConnectionState, MirrorSharpSlowUpdateResult } from 'mirrorsharp';
import { StableCodeEditor } from 'app/source/StableCodeEditor';
import type { FlowStep, Result } from '../ts/types/results';
import type { HighlightedRange } from '../ts/types/highlighted-range';
import type { ServerOptions } from '../ts/types/server-options';
import type { LanguageName } from '../ts/helpers/languages';
import './internal/codemirror/addon-jump-arrows';

export const appCodeEditProps = {
    initialText:       String,
    initialCached:     Boolean,
    serviceUrl:        String,
    language:          String as () => LanguageName|undefined,
    serverOptions:     Object as () => ServerOptions|undefined,
    highlightedRange:  Object as () => HighlightedRange|undefined,
    executionFlow:     Array as () => Array<FlowStep>
};

export default Vue.extend({
    props: appCodeEditProps,
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
    template: `<react-stable-code-editor
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
    ></react-stable-code-editor>`,

    components: {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        'react-stable-code-editor': StableCodeEditor as any
    }
});