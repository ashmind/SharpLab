import '../js/ui/setup/portal-vue.js';
import Vue from 'vue';
import auth from '../js/helpers/github/auth.js';
import { createGistAsync } from '../js/helpers/github/gists.js';

Vue.component('app-gist-manager', {
    props: {
        gist: Object,
        code: String,
        options: Object,
        result: Object
    },
    data: () => ({
        modalOpen: false,
        name: null,
        saving: false,
        error: null
    }),
    computed: {
        canSave() { return !!this.name && !this.saving; }
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
            this.$refs.name.focus();
        },

        async saveAsync() {
            if (!this.name)
                throw new Error('Gist name is required.');

            if (this.saving)
                throw new Error('Gist save already in progress.');

            this.saving = true;
            this.error = null;
            let gist;
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

        handleFormSubmit(e) {
            e.preventDefault();
            this.saveAsync();
        }
    },
    template: '#app-gist-manager'
});

