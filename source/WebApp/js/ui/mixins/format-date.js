import dateFormat from 'dateformat';
import Vue from 'vue';

Vue.mixin({
    methods: {
        formatDate: dateFormat
    }
});