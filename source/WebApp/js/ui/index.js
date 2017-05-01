import Vue from 'vue';
import './mixins/format-date.js';
import hooks from './hooks/registry.js';

import './components/app-favicon-manager.js';
import './components/app-mirrorsharp.js';
import './components/app-mirrorsharp-diagnostic.js';
import './components/app-mirrorsharp-readonly.js';
import './components/app-mobile-shelf.js';
import './hooks/app-mobile-codemirror-fullscreen.js';
import './hooks/app-cloak.js';

const documentReadyPromise = new Promise(resolve => {
    document.addEventListener('DOMContentLoaded', function() {
        resolve();
    });
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
            // jshint -W031
            new Vue({
                el:       'main',
                data:     app.data,
                computed: app.computed,
                methods:  app.methods,
                mounted: function() {
                    Vue.nextTick(() => {
                        for (let hook of hooks.ready) {
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
    return await createUIAsync(app);
}