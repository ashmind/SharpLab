import Vue from 'vue';
import { cases, loadComponentTemplate, renderComponent } from './helpers.js';
import GistManager from '../../components/app-gist-manager.js';

jest.mock('../../js/helpers/github/auth.js');

beforeEach(() => loadComponentTemplate('app-gist-manager'));

test.each(cases)('no gist%s', async (_, bodyClass) => {
    const manager = createManager({ gist: null });

    const rendered = await renderComponent(manager, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(cases)('with gist%s', async (_, bodyClass) => {
    const manager = createManager({
        gist: {
            id: '6c1d9daeabca29e89d197dfb8ea949ef',
            name: 'memorygraph-twitter-demo',
            url: 'https://gist.github.com/6c1d9daeabca29e89d197dfb8ea949ef',
            code: '_',
            options: {}
        }
    });

    const rendered = await renderComponent(manager, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(cases)('modal open%s', async (_, bodyClass) => {
    // @ts-ignore
    const PortalTarget = Vue.options.components.PortalTarget;
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
    // @ts-ignore
    await manager.openModalAsync();

    const rendered = await renderComponent(parent, { bodyClass });

    parent.$destroy();
    expect(rendered).toMatchImageSnapshot();
});

function createManager({ gist }) {
    return new GistManager({
        el: document.createElement('div'),
        propsData: {
            gist
        }
    });
}