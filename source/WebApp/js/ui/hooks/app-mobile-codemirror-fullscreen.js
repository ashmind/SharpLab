import $ from 'jquery';
import registry from './registry';

registry.ready.push(function(vue) {
    const $body = $(vue.$el).find('body');
    const className = $body.data('mobile-codemirror-fullscreen-class');
    $body.find('.CodeMirror').each(function() {
        const cm = this.CodeMirror;
        const $ancestors = $(this).parents();

        cm.on('focus', () => $ancestors.addClass(className));
        cm.on('blur', () => $ancestors.removeClass(className));
    });
});