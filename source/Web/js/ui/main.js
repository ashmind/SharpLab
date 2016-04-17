import Vue from 'vue';
import $ from 'jquery';

import './app-codemirror.js';

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