import { cases, loadComponentTemplate, renderComponent } from './helpers.js';
import OutputView from '../../components/app-output-view.js';

beforeEach(() => loadComponentTemplate('app-output-view'));

test.each(cases)('empty%s', async (_, bodyClass) => {
    const view = createView({ output: [] });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ output }) {
    return new OutputView({
        el: document.createElement('div'),
        propsData: {
            output
        }
    });
}