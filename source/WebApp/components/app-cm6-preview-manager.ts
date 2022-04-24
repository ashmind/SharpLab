import Vue from 'vue';
import { EditorSwitch } from 'app/footer/EditorSwitch';

export default Vue.component('app-cm6-preview-manager', {
    template: `<react-editor-switch></react-editor-switch>`,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    components: { 'react-editor-switch': EditorSwitch as any }
});