import registry from './registry.js';

registry.ready.push(vue => {
    const body = document.getElementsByTagName('body')[0];
    const className = body.dataset.mobileCodemirrorFullscreenClass;
    for (let element of document.querySelectorAll('.CodeMirror')) {
        const cm = element.CodeMirror;
        const ancestors = getAncestors(element, body);
        cm.on('focus', () => ancestors.forEach(a => a.classList.add(className)));
        cm.on('blur',  () => ancestors.forEach(a => a.classList.remove(className)));
    }

    function getAncestors(element, body) {
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