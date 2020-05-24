import AstView from '../../components/app-ast-view';
import { themeCases, loadComponentTemplate, renderComponent, PickPropTypes } from './helpers';
import rootsSimple from './data/ast-roots-simple';

beforeEach(() => {
    loadComponentTemplate('app-ast-view-item', 'internal');
    Element.prototype.scrollIntoView = () => {};
});

test.each(themeCases)('empty%s', async (_, bodyClass) => {
    const view = createView({ roots: [] });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('simple tree%s', async (_, bodyClass) => {
    const view = createView({ roots: rootsSimple });
    view.selectDeepestByOffset(58); // space before }

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ roots }: PickPropTypes<typeof AstView, 'roots'>) {
    return new AstView({
        el: document.createElement('div'),
        propsData: {
            roots
        }
    });
}