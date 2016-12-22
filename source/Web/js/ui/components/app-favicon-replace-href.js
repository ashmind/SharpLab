import $ from 'jquery';
import Vue from 'vue';

const savedHrefKey = 'app-favicon-replace-href::saved';

Vue.component('app-favicon-replace-href', {
    props: {
        if: Boolean,
        regexp: String,
        with: String
    },
    ready: function() {
        const favicons = document.querySelectorAll('link[rel=icon]');

        this.$watch('if', value => {
            if (value) {
                for (let favicon of favicons) {
                    const href = favicon.href;
                    favicon[savedHrefKey] = href;
                    favicon.href = href.replace(new RegExp(this.regexp), this.with);
                }
            }
            else {
                for (let favicon of favicons) {
                    const saved = favicon[savedHrefKey];
                    if (saved)
                        favicon.href = saved;
                }
            }
        });
    }
});