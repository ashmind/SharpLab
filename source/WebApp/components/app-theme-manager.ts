import Vue from 'vue';
import type { AppTheme } from '../ts/types/app';
import { getUserTheme, setUserTheme } from '../ts/helpers/theme';
import asLookup from '../ts/helpers/as-lookup';

const component = Vue.component('app-theme-manager', {
    data: () => ({
        theme: 'auto' as AppTheme
    }),
    computed: {
        themeLabel() {
            return ({
                auto:  'Auto',
                dark:  'Dark',
                light: 'Light'
            } as const)[this.theme];
        }
    },
    mounted() {
        this.theme = getUserTheme();
        updateBodyClass(this.theme);
    },
    methods: {
        toggle() {
            const next = ({
                auto:  'dark',
                dark:  'light',
                light: 'auto'
            } as const)[this.theme];
            this.theme = next;

            setUserTheme(this.theme);
            updateBodyClass(this.theme);
        }
    },
    template: '#app-theme-manager'
});

function updateBodyClass(theme: AppTheme) {
    const allClasses = ['theme-dark', 'theme-auto'] as const;
    const newClassName = asLookup({
        dark: 'theme-dark',
        auto: 'theme-auto'
    } as const)[theme];

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