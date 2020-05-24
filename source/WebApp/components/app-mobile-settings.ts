import Vue from 'vue';
import type { AppOptions } from '../ts/types/app';
import type { Branch } from '../ts/types/branch';
import withRefsType from '../ts/helpers/with-refs-type';
import { uid } from '../ts/ui/helpers/uid';
import type AppModal from './internal/app-modal';
// eslint-disable-next-line no-duplicate-imports
import './internal/app-modal';
import './app-select-language';
import './app-select-branch';
import './app-section-branch-details';
import './app-select-target';
import './app-select-mode';
import './app-cm6-preview-manager';

export default withRefsType<{
    modal: InstanceType<typeof AppModal>;
}>(Vue).component('app-mobile-settings', {
    props: {
        options: Object as () => AppOptions,
        branches: Array as () => ReadonlyArray<Branch>
    },
    data: () => ({
        id: uid()
    }),
    methods: {
        openModal() {
            this.$refs.modal.open();
        }
    },
    template: '#app-mobile-settings'
});