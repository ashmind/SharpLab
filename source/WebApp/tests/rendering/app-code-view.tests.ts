import Vue from 'vue';
import CodeView from '../../components/app-code-view';
import { themeCases, renderComponent, PickPropTypes } from './helpers';
import { basicCSharp } from './data/code';

beforeEach(() => {
    // eslint-disable-next-line no-undefined
    Range.prototype.getBoundingClientRect = () => undefined as unknown as DOMRect;
    Range.prototype.getClientRects = () => [] as unknown as DOMRectList;
    Object.defineProperty(HTMLElement.prototype, 'offsetWidth', {
        get(this: HTMLElement) {
            switch (this.className) {
                case 'CodeMirror-gutters': return 20;
                case 'CodeMirror cm-s-default': return 100;
                default: return 0;
            }
        }
    });
});

test.each(themeCases)('empty%s', async (_, bodyClass) => {
    const view = createView({ value: '', language: 'C#', ranges: [] });
    await Vue.nextTick();

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('basic C#%s', async (_, bodyClass) => {
    // TODO: fix height
    const view = createView({
        value: basicCSharp,
        language: 'C#',
        ranges: []
    });
    await Vue.nextTick();

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ value, language, ranges }: PickPropTypes<typeof CodeView, 'value'|'language'|'ranges'>) {
    return new CodeView({
        el: document.createElement('div'),
        propsData: {
            value,
            language,
            ranges
        }
    });
}