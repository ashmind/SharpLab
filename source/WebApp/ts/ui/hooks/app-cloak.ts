import { allHooks } from './registry';

allHooks.main.ready.push(() => {
    for (const element of document.querySelectorAll('[data-cloak]')) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        element.classList.remove((element as HTMLElement).dataset.cloak!);
    }
});