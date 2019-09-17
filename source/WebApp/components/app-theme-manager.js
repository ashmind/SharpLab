import Vue from 'vue';
import { save, load } from '../js/state/theme.js';
import trackFeature from '../js/helpers/track-feature.js';

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
        trackDarkTheme(this.theme);
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

function trackDarkTheme(theme) {
    switch (theme) {
        case 'dark': {
            trackFeature('Theme: Dark (manual)');
            return;
        }
        case 'auto': {
            const systemDarkTheme = window.matchMedia
                && window.matchMedia('(prefers-color-scheme: dark)').matches;
            if (systemDarkTheme)
                trackFeature('Theme: Dark (system)');
            return; // eslint-disable-line no-useless-return
        }
    }
}

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