import $ from 'jquery';
import Hammer from 'hammerjs';
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
        
        if ($container) {
            Hammer($container[0])
                .on('swipeleft', () => {
                    $container.removeClass(this.openClass);
                })
                .on('swiperight', () => {
                    $container.addClass(this.openClass);
                });
        }
    },
    template: '<div></div>'
});