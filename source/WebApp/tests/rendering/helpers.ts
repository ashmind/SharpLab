/* eslint-disable import/no-duplicates */
import type Vue from 'vue';
import type { VueConstructor } from 'vue';
/* eslint-enable import/no-duplicates */
import fs from 'fs';
import render from './render';

export type PickPropTypes<TVueConstructor extends VueConstructor, TKey extends keyof InstanceType<TVueConstructor>> =
    Pick<InstanceType<TVueConstructor>, TKey>;

const styles = [
    { path: `${__dirname}/../../wwwroot/app.min.css` }
] as const;

export const cases = [
    ['', ''],
    [' (dark)', 'theme-dark']
] as const;

export function loadComponentTemplate(id: string, subDirectory = '') {
    const template = document.createElement('script');
    template.id = id;
    template.type = 'text/x-template';
    template.textContent = fs.readFileSync(`${__dirname}/../../components/${subDirectory}/${id}.html`, { encoding: 'utf-8' });
    document.body.appendChild(template);
}

export function renderComponent(view: Vue, options: {
    wrap?: (html: string) => string;
    bodyClass?: string;
} = {}) {
    let html = view.$el.outerHTML;
    const { wrap, ...renderOptions } = options;
    if (wrap)
        html = wrap(html);

    return render({ html, styles, ...renderOptions });
}