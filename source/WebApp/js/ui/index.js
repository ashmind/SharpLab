import Vue from 'vue';
import './mixins/format-date.js';
import './mixins/markdown.js';
import hooks from './hooks/registry.js';
import '../../components/index.js';
import './directives/app-class-toggle.js';
import './hooks/app-cloak.js';
import './hooks/app-mobile-codemirror-fullscreen.js';
import './hooks/app-theme-color-manager.js';

const documentReadyPromise = new Promise(resolve => {
    document.addEventListener('DOMContentLoaded', () => resolve());
});

async function createUIAsync(app) {
    const main = await createTopLevelUIComponentAsync(app, 'main', hooks.main);
    await createTopLevelUIComponentAsync(app, 'main + footer', hooks.footer);

    return main;
}

function createTopLevelUIComponentAsync(app, selector, specificHooks = {}) {
    return new Promise((resolve, reject) => {
        try {
            // ReSharper disable once ConstructorCallNotUsed
            new Vue({
                el:       selector,
                data:     app.data,
                computed: app.computed,
                methods:  app.methods,
                mounted: function() {
                    Vue.nextTick(() => {
                        for (const hook of specificHooks.ready || []) {
                            hook(this);
                        }
                        resolve(wrap(this));
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

export default async function(app) {
    await documentReadyPromise;
    return createUIAsync(app);
}