import VerifyView from '../../components/app-verify-view';
import { themeCases, renderComponent, PickPropTypes } from './helpers';

test.each(themeCases)('success%s', async (_, bodyClass) => {
    const view = createView({ value: '✔️ Compilation completed.' });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ value }: PickPropTypes<typeof VerifyView, 'value'>) {
    return new VerifyView({
        el: document.createElement('div'),
        propsData: { value }
    });
}