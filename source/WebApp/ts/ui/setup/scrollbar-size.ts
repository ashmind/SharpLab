// https://davidwalsh.name/detect-scrollbar-width
const container = document.createElement('div');
Object.assign(container.style, {
    position: 'absolute',
    width: '100px',
    height: '100px',
    overflow: 'scroll',
    left: '-9999px'
});

document.body.appendChild(container);
const scrollbarSize = container.offsetWidth - container.clientWidth;
document.body.removeChild(container);

document.documentElement.style.setProperty('--js-scrollbar-width', scrollbarSize + 'px');

export {};