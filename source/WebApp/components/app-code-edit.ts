import type { ExtendedVue } from 'vue/types/vue';
import Vue from 'vue';
import { cm6PreviewState } from './state/cm6-preview';
import AppCodeEditStable, { appCodeEditProps } from './app-code-edit-stable';
import AppCodeEditCM6Preview from './app-code-edit-cm6-preview';

export default Vue.component('app-code-edit', {
    props: appCodeEditProps,
    data: () => ({
        preview: cm6PreviewState
    }),
    computed: {
        editor(): ExtendedVue<Vue, unknown, unknown, unknown, unknown> {
            return this.preview.enabled ? AppCodeEditCM6Preview : AppCodeEditStable;
        }
    },
    template: '#app-code-edit'
});