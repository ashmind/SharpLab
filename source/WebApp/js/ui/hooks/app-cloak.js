import registry from './registry.js';

registry.ready.push(vue => {
    for (let element of document.querySelectorAll('[data-cloak]')) {
        element.classList.remove(element.dataset.cloak);
    }
});