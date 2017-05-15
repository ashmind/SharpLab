import registry from './registry.js';

registry.ready.push(() => {
    for (const element of document.querySelectorAll('[data-cloak]')) {
        element.classList.remove(element.dataset.cloak);
    }
});