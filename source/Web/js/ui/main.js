import Vue from 'vue';
import $ from 'jquery';
import hooks from './hooks/registry';

import './filters/app-date';
import './filters/app-trim';
import './components/app-codemirror';
import './components/app-diagnostic';
import './hooks/app-mobile-codemirror-fullscreen';

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
                    }
                });
            }
            catch (e) {
                reject(e);
                return;
            }
            resolve();
        });
    });
}