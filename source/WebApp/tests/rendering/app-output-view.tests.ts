import OutputView from '../../components/app-output-view';
import { cases, loadComponentTemplate, renderComponent, PickPropTypes } from './helpers';

beforeEach(() => loadComponentTemplate('app-output-view'));

test.each(cases)('empty%s', async (_, bodyClass) => {
    const view = createView({ output: [] });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ output }: PickPropTypes<typeof OutputView, 'output'>) {
    return new OutputView({
        el: document.createElement('div'),
        propsData: {
            output
        }
    });
}