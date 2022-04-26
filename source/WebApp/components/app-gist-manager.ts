import Vue from 'vue';
import { GistManager, GistSaveContext } from 'app/header/GistManager';
import type { AppOptions } from '../ts/types/app';
import type { Result } from '../ts/types/results';
import type { Gist } from '../ts/types/gist';

type ButtonProps = {
    class?: string;
    tabindex: number;
    [key: string]: unknown;
};

export default (Vue).component('app-gist-manager', {
    props: {
        gist: Object as () => Gist|null,
        code: String,
        options: Object as () => AppOptions,
        result: Object as () => Result,

        useLabel: Boolean,
        buttonProps: {
            default: () => ({} as ButtonProps),
            type: Object as () => ButtonProps
        }
    },
    computed: {
        context(): GistSaveContext {
            const { code, options, result } = this;
            return { code, options, result };
        },

        reactButtonProps(): Record<string, unknown> {
            const { class: className, tabindex: tabIndex, ...rest } = this.buttonProps;
            return { className, tabIndex, ...rest };
        }
    },
    methods: {
        onSave(gist: Gist) {
            this.$emit('save', gist);
        }
    },
    template: `<react-gist-manager
        class="temp-react-wrapper"
        v-bind:gist="gist"
        v-bind:context="context"
        v-bind:useLabel="useLabel"
        v-bind:buttonProps="reactButtonProps"
        v-on:onSave="onSave"></react-gist-manager>`,
    components: {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        'react-gist-manager': GistManager as any
    }
});