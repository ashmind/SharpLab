import Vue from 'vue';
import { uid } from '../ts/ui/helpers/uid';
import { cm6PreviewEnabled, setCM6PreviewEnabled } from './state/cm6-preview';

export default Vue.component('app-cm6-preview-manager', {
    data: () => ({
        enabled: cm6PreviewEnabled,
        // eslint-disable-next-line no-plusplus
        id: uid()
    }),
    computed: {
        editorName() {
            return this.enabled.value ? 'Preview' : 'Default';
        }
    },
    methods: {
        toggle() {
            setCM6PreviewEnabled(!this.enabled.value);
        }
    },
    template: '#app-cm6-preview-manager'
});