import marked from 'marked';
import Vue from 'vue';

Vue.mixin({
    methods: {
        markdown: x => {
            const m = marked(x);
            return m;
        }
    }
});