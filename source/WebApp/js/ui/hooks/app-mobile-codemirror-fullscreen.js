import registry from './registry.js';

registry.main.ready.push(() => {
    const body = document.getElementsByTagName('body')[0];
    const className = body.dataset.mobileCodemirrorFullscreenClass;
    for (const element of document.querySelectorAll('.CodeMirror')) {
        const cm = element.CodeMirror;
        const ancestors = getAncestors(element);
        cm.on('focus', () => ancestors.forEach(a => a.classList.add(className)));
        cm.on('blur',  () => ancestors.forEach(a => a.classList.remove(className)));
    }

    function getAncestors(element) {
        const ancestors = [];
        let parent = element.parentNode;
        while (parent) {
            ancestors.push(parent);
            if (parent === body)
                break;
            parent = parent.parentNode;
        }
        return ancestors;
    }
});