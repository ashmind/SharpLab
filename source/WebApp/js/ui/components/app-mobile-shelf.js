import $ from 'jquery';
import Vue from 'vue';

Vue.component('app-mobile-shelf', {
    props: {
        container: String,
        toggle:    String,
        openClass: String
    },
    ready: function() {
        const $container = this.container ? $(this.container) : null;
        const $classChangeTarget = $container || $(this.$el);

        $(this.toggle).click(() => {
            $classChangeTarget.toggleClass(this.openClass);
        });
    },
    template: '<div></div>'
});