import Vue from 'vue';
import $ from 'jquery';

import './filters/app-date.js';
import './filters/app-trim.js';
import './app-codemirror.js';
import './app-diagnostic.js';

export default function(model) {    
    return new Promise(function(resolve, reject) {
        $(function() {
            try {
                // jshint -W031
                new Vue({
                    el: 'body',
                    data: model
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