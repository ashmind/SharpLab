import fs from 'fs';
import render from './render.js';

const styles = [
    { path: `${__dirname}/../../wwwroot/app.min.css` }
];

export const cases = [
    ['', ''],
    [' (dark)', 'theme-dark']
];

export function loadComponentTemplate(id, subDirectory = '') {
    const template = document.createElement('script');
    template.id = id;
    template.type = 'text/x-template';
    template.textContent = fs.readFileSync(`${__dirname}/../../components/${subDirectory}/${id}.html`, { encoding: 'utf-8' });
    document.body.appendChild(template);
}

export function renderComponent(view, options = {}) {
    let html = view.$el.outerHTML;
    const { wrap, ...renderOptions } = options;
    if (wrap)
        html = wrap(html);

    return render({ html, styles, ...renderOptions });
}