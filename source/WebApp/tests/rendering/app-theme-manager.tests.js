import { loadComponentTemplate, cases, renderComponent } from './helpers.js';
import ThemeManager from '../../components/app-theme-manager.js';

beforeEach(() => loadComponentTemplate('app-theme-manager'));

test.each(cases)('auto%s', async (_, bodyClass) => {
    const view = createManager();

    const rendered = await renderComponent(view, {
        wrap: html => `<main></main><footer>${html}</footer>`,
        bodyClass
    });

    expect(rendered).toMatchImageSnapshot();
});

function createManager() {
    return new ThemeManager({
        el: document.createElement('div')
    });
}