import Vue, { VueConstructor } from 'vue';
import GistManager from '../../components/app-gist-manager';
import { fromPartial } from '../helpers';
import { themeCases, loadComponentTemplate, renderComponent, PickPropTypes } from './helpers';

jest.mock('../../ts/helpers/github/auth');

beforeEach(() => {
    loadComponentTemplate('app-modal', 'internal');
    loadComponentTemplate('app-gist-manager');
});

test.each(themeCases)('no gist%s', async (_, bodyClass) => {
    const manager = createManager({ gist: null });

    const rendered = await renderComponent(manager, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('with gist%s', async (_, bodyClass) => {
    const manager = createManager({
        gist: {
            id: '6c1d9daeabca29e89d197dfb8ea949ef',
            name: 'memorygraph-twitter-demo',
            url: 'https://gist.github.com/6c1d9daeabca29e89d197dfb8ea949ef',
            code: '_',
            options: fromPartial({})
        }
    });

    const rendered = await renderComponent(manager, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('modal open%s', async (_, bodyClass) => {
    const PortalTarget = (Vue as unknown as { options: { components: { PortalTarget: VueConstructor } } }).options.components.PortalTarget;
    const parent = new Vue({
        el: document.createElement('div'),
        render(h) {
            return h('div', [
                h(GistManager),
                h(PortalTarget, { props: { name: 'modals', multiple: true, slim: true } })
            ]);
        }
    });

    const manager = parent.$children[0];
    await (manager as InstanceType<typeof GistManager>).openModalAsync();

    const rendered = await renderComponent(parent, { bodyClass });

    parent.$destroy();
    expect(rendered).toMatchImageSnapshot();
});

function createManager({ gist }: PickPropTypes<typeof GistManager, 'gist'>) {
    return new GistManager({
        el: document.createElement('div'),
        propsData: {
            gist,
            buttonClass: 'header-text-button'
        }
    });
}