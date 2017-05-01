import Vue from 'vue';
import hooks from './hooks/registry.js';

import './filters/app-date.js';
import './filters/app-trim.js';
import './components/app-favicon-manager.js';
import './components/app-loader.js';
import './components/app-mirrorsharp.js';
import './components/app-mirrorsharp-diagnostic.js';
import './components/app-mirrorsharp-readonly.js';
import './components/app-mobile-shelf.js';
import './hooks/app-mobile-codemirror-fullscreen.js';

function wrap(vue) {
    return {
        watch: (name, callback, options) => {
            vue.$watch(name, callback, options);
        }
    };
}

export default function(app) {
    return new Promise(function(resolve, reject) {
        document.addEventListener('DOMContentLoaded', () => {
            try {
                // ReSharper disable once ConstructorCallNotUsed
                // jshint -W031
                new Vue({
                    el: 'html',
                    data: app.data,
                    computed: app.computed,
                    methods: app.methods,
                    ready: function() {
                        for (let hook of hooks.ready) {
                            hook(this);
                        }
                        const ui = wrap(this);
                        resolve(ui);
                    }
                });
            }
            catch (e) {
                reject(e);
            }
        });
    });
}