import Vue from 'vue';
import './mixins/format-date.js';
import './mixins/markdown.js';
import hooks from './hooks/registry.js';
import './components/app-code-edit.js';
import './components/app-diagnostic.js';
import './components/app-code-view.js';
import './components/app-ast-view.js';
import './components/app-verify-view.js';
import './components/app-explain-view.js';
import './components/app-output-view.js';
import './directives/app-class-toggle.js';
import './hooks/app-cloak.js';
import './hooks/app-mobile-codemirror-fullscreen.js';
import './hooks/app-theme-color-manager.js';

const documentReadyPromise = new Promise(resolve => {
    document.addEventListener('DOMContentLoaded', () => resolve());
});

function createUIAsync(app) {
    return new Promise((resolve, reject) => {
        try {
            // ReSharper disable once ConstructorCallNotUsed
            new Vue({
                el:       'main',
                data:     app.data,
                computed: app.computed,
                methods:  app.methods,
                mounted: function() {
                    Vue.nextTick(() => {
                        for (const hook of hooks.ready) {
                            hook(this);
                        }
                        attachToFooter();
                        const ui = wrap(this);
                        resolve(ui);
                    });
                }
            });
        }
        catch (e) {
            reject(e);
        }
    });
}

function wrap(vue) {
    return {
        watch: (name, callback, options) => {
            vue.$watch(name, callback, options);
        }
    };
}

function attachToFooter() {
    // since footer is outside of <main>, I have to handle the events manually
    const body = document.querySelector('body');
    const themeToggle = document.querySelector('body > footer [data-manual-role=toggle-theme]');
    themeToggle.addEventListener('click', () => body.classList.toggle('theme-dark'));
}

export default async function(app) {
    await documentReadyPromise;
    return createUIAsync(app);
}