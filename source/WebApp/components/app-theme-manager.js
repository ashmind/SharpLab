import Vue from 'vue';
import { save, load } from '../js/state/theme.js';

const component = Vue.component('app-theme-manager', {
    data: () => ({
        theme: 'auto'
    }),
    computed: {
        themeLabel() {
            return {
                auto:  'Auto',
                dark:  'Dark',
                light: 'Light'
            }[this.theme];
        }
    },
    mounted() {
        this.theme = load() || 'auto';
        updateBodyClass(this.theme);
    },
    methods: {
        toggle() {
            const next = {
                auto:  'dark',
                dark:  'light',
                light: 'auto'
            }[this.theme];
            this.theme = next;

            save(this.theme);
            updateBodyClass(this.theme);
        }
    },
    template: '#app-theme-manager'
});

function updateBodyClass(theme) {
    const allClasses = ['theme-dark', 'theme-auto'];
    const newClassName = {
        dark: 'theme-dark',
        auto: 'theme-auto'
    }[theme];

    const body = document.body;
    for (const className of allClasses) {
        if (className === newClassName) {
            body.classList.add(className);
        }
        else {
            body.classList.remove(className);
        }
    }
}

export default component;