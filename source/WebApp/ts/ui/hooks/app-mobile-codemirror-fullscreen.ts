import { allHooks } from './registry';

allHooks.main.ready.push(() => {
    const body = document.getElementsByTagName('body')[0];
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const className = body.dataset.mobileCodemirrorFullscreenClass!;
    for (const element of document.querySelectorAll('.CodeMirror')) {
        const cm = (element as unknown as { CodeMirror: CodeMirror.Editor }).CodeMirror;
        const ancestors = getAncestors(element);
        cm.on('focus', () => ancestors.forEach(a => a.classList.add(className)));
        cm.on('blur',  () => ancestors.forEach(a => a.classList.remove(className)));
    }

    function getAncestors(element: Element) {
        const ancestors = [] as Array<Element>;
        let parent = element.parentNode;
        while (parent) {
            ancestors.push(parent as Element);
            if (parent === body)
                break;
            parent = parent.parentNode;
        }
        return ancestors;
    }
});