import registry from './registry.js';

registry.main.ready.push(() => {
    for (const element of /** @type {NodeListOf<HTMLElement>} */(document.querySelectorAll('[data-cloak]'))) {
        element.classList.remove(element.dataset.cloak);
    }
});