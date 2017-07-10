import Vue from 'vue';
import './mixins/format-date.js';
import hooks from './hooks/registry.js';

import './components/app-mirrorsharp.js';
import './components/app-mirrorsharp-diagnostic.js';
import './components/app-mirrorsharp-readonly.js';
import './components/app-ast-view.js';
import './components/app-run-view.js';
import './directives/app-class-toggle.js';
import './hooks/app-cloak.js';
import './hooks/app-mobile-codemirror-fullscreen.js';
import './hooks/app-theme-color-manager.js';

const documentReadyPromise = new Promise(resolve => {
    document.addEventListener('DOMContentLoaded', () => resolve());
});

function wrap(vue) {
    return {
        watch: (name, callback, options) => {
            vue.$watch(name, callback, options);
        }
    };
}

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

export default async function(app) {
    await documentReadyPromise;
    return createUIAsync(app);
}