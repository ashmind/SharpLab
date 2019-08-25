import { cases, renderComponent } from './helpers.js';
import VerifyView from '../../components/app-verify-view.js';

test.each(cases)('success%s', async (_, bodyClass) => {
    const view = createView({ value: '✔️ Compilation completed.' });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ value }) {
    return new VerifyView({
        el: document.createElement('div'),
        propsData: { value }
    });
}