import fs from 'fs';
import path from 'path';
import type Vue from 'vue';
// eslint-disable-next-line no-duplicate-imports
import type { VueConstructor } from 'vue';
import render from './render';

export type PickPropTypes<TVueConstructor extends VueConstructor, TKey extends keyof InstanceType<TVueConstructor>> =
    Pick<InstanceType<TVueConstructor>, TKey>;

const publicRootPath = path.resolve(`${__dirname}/../../public`);
// eslint-disable-next-line no-sync
const indexHtmlPath = fs.readFileSync(`${publicRootPath}/latest`, { encoding: 'utf-8' });
const styles = [
    { path: `${publicRootPath}/${path.basename(indexHtmlPath)}/app.min.css` }
] as const;

export const themeCases = [
    ['', ''],
    [' (dark)', 'theme-dark']
] as const;

export const themeAndStatusCases = [
    ['', ''],
    [' (error)', 'root-status-error'],
    [' (offline)', 'root-status-offline'],
    [' (dark)', 'theme-dark'],
    [' (dark, error)', 'theme-dark root-status-error'],
    [' (dark, offline)', 'theme-dark root-status-offline']
];

export const mobilePortraitSize = { width: 400, height: 800 };

export function loadComponentTemplate(id: string, subDirectory = '') {
    const template = document.createElement('script');
    template.id = id;
    template.type = 'text/x-template';
    // eslint-disable-next-line no-sync
    template.textContent = fs.readFileSync(`${__dirname}/../../components/${subDirectory}/${id}.html`, { encoding: 'utf-8' });
    document.body.appendChild(template);
}

type RenderOptions = Parameters<typeof render>[0];

export function renderComponent(view: Vue, options: {
    wrap?: (html: string) => string;
    allowEmpty?: boolean;
} & Omit<RenderOptions, 'html'> = {}) {
    let html = view.$el.outerHTML as string|undefined;
    if (!html) {
        if (!options.allowEmpty) {
            // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
            throw new Error(`Failed to render component (html: ${html})`);
        }
        html = '';
    }

    const { wrap, ...renderOptions } = options;
    if (wrap)
        html = wrap(html);

    return render({ html, styles, ...renderOptions });
}