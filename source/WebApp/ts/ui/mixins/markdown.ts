import { marked } from 'marked';
import Vue from 'vue';

Vue.mixin({
    methods: {
        markdown: marked
    }
});