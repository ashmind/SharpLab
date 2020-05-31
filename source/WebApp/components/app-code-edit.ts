import type { ExtendedVue } from 'vue/types/vue';
import Vue from 'vue';
import { cm6PreviewEnabled } from './state/cm6-preview';
import AppCodeEditStable, { appCodeEditProps } from './app-code-edit-stable';
import AppCodeEditCM6Preview from './app-code-edit-cm6-preview';

export default Vue.component('app-code-edit', {
    props: appCodeEditProps,
    data: () => ({
        previewEnabled: cm6PreviewEnabled
    }),
    computed: {
        editor(): ExtendedVue<Vue, unknown, unknown, unknown, unknown> {
            return this.previewEnabled.value ? AppCodeEditCM6Preview : AppCodeEditStable;
        }
    },
    template: '#app-code-edit'
});