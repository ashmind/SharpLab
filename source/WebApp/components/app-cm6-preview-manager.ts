import Vue from 'vue';
import { uid } from '../ts/ui/helpers/uid';
import { cm6PreviewState, setCM6PreviewEnabled } from './state/cm6-preview';

export default Vue.component('app-cm6-preview-manager', {
    data: () => ({
        preview: cm6PreviewState,
        // eslint-disable-next-line no-plusplus
        id: uid()
    }),
    computed: {
        editorName() {
            return this.preview.enabled ? 'Preview' : 'Default';
        }
    },
    methods: {
        toggle() {
            setCM6PreviewEnabled(!this.preview.enabled);
        }
    },
    template: '#app-cm6-preview-manager'
});