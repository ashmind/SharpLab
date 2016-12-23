import Vue from 'vue';
import $ from 'jquery';
import hooks from './hooks/registry';

import './filters/app-date';
import './filters/app-trim';
import './components/app-codemirror';
import './components/app-mirrorsharp';
import './components/app-diagnostic';
import './components/app-favicon-replace-href';
import './components/app-loader';
import './components/app-mobile-shelf';
import './hooks/app-mobile-codemirror-fullscreen';

function wrap(vue) {
    return {
        watch: (name, callback, options) => {
            vue.$watch(name, callback, options);
        }
    };
}

export default function(model) {
    return new Promise(function(resolve, reject) {
        $(function() {
            try {
                // jshint -W031
                new Vue({
                    el: 'body',
                    data: model,
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