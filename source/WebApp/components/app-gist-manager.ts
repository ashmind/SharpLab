import Vue from 'vue';
import type { AppOptions } from '../ts/types/app';
import type { Result } from '../ts/types/results';
import type { Gist } from '../ts/types/gist';
import withRefsType from '../ts/helpers/with-refs-type';
import auth from '../ts/helpers/github/auth';
import { createGistAsync } from '../ts/helpers/github/gists';
import toRawOptions from '../ts/helpers/to-raw-options';
import { uid } from '../ts/ui/helpers/uid';
import type AppModal from './internal/app-modal';
// eslint-disable-next-line no-duplicate-imports
import './internal/app-modal';

// only doing it once per page load, even if
// multiple app-gist-managers are created
let postAuthRedirectModalOpened = false;

export default withRefsType<{
    modal: InstanceType<typeof AppModal>;
    name: HTMLInputElement;
}>(Vue).component('app-gist-manager', {
    props: {
        gist: Object as () => Gist|null,
        code: String,
        options: Object as () => AppOptions,
        result: Object as () => Result,

        useLabel: Boolean,
        buttonClass: String as () => string|null
    },
    data: () => ({
        id: uid(),
        name: null as string|null,
        saving: false,
        error: null
    }),
    computed: {
        canSave(): boolean { return !!this.name && !this.saving; }
    },
    async mounted() {
        if (auth.isBackFromRedirect && !postAuthRedirectModalOpened) {
            postAuthRedirectModalOpened = true;
            await this.openModalAsync();
        }
    },
    methods: {
        async openModalAsync() {
            await auth.redirectIfRequiredAsync();
            this.$refs.modal.open();
            await Vue.nextTick();
            this.$refs.name.focus();
        },

        async saveAsync() {
            if (!this.name)
                throw new Error('Gist name is required.');

            if (this.saving)
                throw new Error('Gist save already in progress.');

            this.saving = true;
            this.error = null;
            let gist: Gist;
            try {
                gist = await createGistAsync({
                    name: this.name,
                    code: this.code,
                    options: toRawOptions(this.options),
                    result: this.result
                });
            }
            catch (e) {
                this.error = (e as { message?: string }).message ?? e;
                this.saving = false;
                return;
            }
            this.$refs.modal.close();
            this.$emit('save', gist);
            this.saving = false;
        },

        handleModalClose() {
            this.name = null;
            this.error = null;
        },

        handleFormSubmit(e: Event) {
            e.preventDefault();
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            this.saveAsync();
        }
    },
    template: '#app-gist-manager'
});