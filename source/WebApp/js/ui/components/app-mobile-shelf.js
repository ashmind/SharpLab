import Vue from 'vue';

Vue.component('app-mobile-shelf', {
    props: {
        container: String,
        toggle:    String,
        openClass: String
    },
    mounted: function() {
        Vue.nextTick(() => {
            const $ = s => document.querySelector(s);
            const classChangeTarget = this.container ? $(this.container) : this.$el;
            $(this.toggle).addEventListener('click', () => {
                classChangeTarget.classList.toggle(this.openClass);
            });
        });
    },
    template: '<div></div>'
});