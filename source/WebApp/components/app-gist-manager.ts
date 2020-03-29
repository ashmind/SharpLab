import Vue from 'vue';
import '../ts/ui/setup/portal-vue';
import type { AppOptions } from '../ts/types/app';
import type { Result } from '../ts/types/results';
import type { Gist } from '../ts/types/gist';
import extendType from '../ts/helpers/extend-type';
import auth from '../ts/helpers/github/auth';
import { createGistAsync } from '../ts/helpers/github/gists';

export default Vue.component('app-gist-manager', {
    props: {
        gist: Object as () => Gist|null,
        code: String,
        options: Object as () => AppOptions,
        result: Object as () => Result
    },
    data: () => extendType({
        modalOpen: false,
        name: null as string|null,
        saving: false,
        error: null
    })<{
        escListener: (e: KeyboardEvent) => void;
    }>(),
    computed: {
        canSave(): boolean { return !!this.name && !this.saving; }
    },
    mounted() {
        if (auth.isBackFromRedirect)
            this.openModalAsync();
    },
    methods: {
        async openModalAsync() {
            this.escListener = e => {
                if (e.key === 'Escape')
                    this.closeModal();
            };
            await auth.redirectIfRequiredAsync();
            this.modalOpen = true;
            document.addEventListener('keyup', this.escListener);
            await Vue.nextTick();
            (this.$refs.name as HTMLInputElement).focus();
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
                    options: this.options,
                    result: this.result
                });
            }
            catch (e) {
                this.error = e.message || e;
                this.saving = false;
                return;
            }
            this.closeModal();
            this.$emit('save', gist);
            this.saving = false;
        },

        closeModal() {
            this.modalOpen = false;
            this.name = null;
            this.error = null;
            document.removeEventListener('keyup', this.escListener);
        },

        handleFormSubmit(e: Event) {
            e.preventDefault();
            this.saveAsync();
        }
    },
    template: '#app-gist-manager'
});

