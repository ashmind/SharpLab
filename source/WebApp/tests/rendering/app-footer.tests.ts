import Footer from '../../components/app-footer';
import { loadComponentTemplate, themeCases, renderComponent, mobilePortraitSize } from './helpers';

beforeEach(() => {
    loadComponentTemplate('app-cm6-preview-manager');
    loadComponentTemplate('app-mobile-font-size-manager');
    loadComponentTemplate('app-theme-manager');
    loadComponentTemplate('app-footer');
});

test.each(themeCases)('desktop%s', async (_, bodyClass) => {
    const view = createView();

    const rendered = await renderComponent(view, { bodyClass, wrap });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('mobile%s', async (_, bodyClass) => {
    const view = createView();

    const rendered = await renderComponent(view, { bodyClass, wrap, ...mobilePortraitSize });

    expect(rendered).toMatchImageSnapshot();
});

function createView() {
    return new Footer({
        el: document.createElement('footer')
    });
}

function wrap(footerHtml: string) {
    return '<main></main>' + footerHtml;
}