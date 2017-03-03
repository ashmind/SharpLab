import Vue from 'vue';
import $ from 'jquery';
import hooks from './hooks/registry';

import './filters/app-date';
import './filters/app-trim';
import './components/app-favicon-manager';
import './components/app-loader';
import './components/app-mirrorsharp';
import './components/app-mirrorsharp-diagnostic';
import './components/app-mirrorsharp-readonly';
import './components/app-mobile-shelf';
import './hooks/app-mobile-codemirror-fullscreen';

function wrap(vue) {
    return {
        watch: (name, callback, options) => {
            vue.$watch(name, callback, options);
        }
    };
}

export default function(app) {
    return new Promise(function(resolve, reject) {
        $(function() {
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