import { allHooks } from './registry';

allHooks.main.ready.push(() => {
    for (const element of document.querySelectorAll('[data-cloak]') as NodeListOf<HTMLElement>) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        element.classList.remove(element.dataset.cloak!);
    }
});