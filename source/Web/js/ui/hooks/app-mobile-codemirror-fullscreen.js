import $ from 'jquery';
import Vue from 'vue';
import registry from './registry';

registry.ready.push(function(vue) {
    const $root = $(vue.$el);
    const className = $root.data('mobile-codemirror-fullscreen-class');
    $root.find('.CodeMirror').each(function() {
        const cm = this.CodeMirror;
        const $ancestors = $(this).parents();

        cm.on('focus', () => $ancestors.addClass(className));
        cm.on('blur', () => $ancestors.removeClass(className));
    });
});