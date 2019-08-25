import Vue from 'vue';
import { cases, renderComponent } from './helpers.js';
import CodeView from '../../components/app-code-view.js';
import { basicCSharp } from './data/code.js';

beforeEach(() => {
    class MockTextRange {
        getBoundingClientRect() {}
        getClientRects() { return []; }
    }

    document.body.createTextRange = () => new MockTextRange();
    Object.defineProperty(HTMLElement.prototype, 'offsetWidth', {
        get() {
            switch (this.className) {
                case 'CodeMirror-gutters': return 20;
                case 'CodeMirror cm-s-default': return 100;
                default: return 0;
            }
        }
    });
});

test.each(cases)('empty%s', async (_, bodyClass) => {
    const view = createView({ value: '', language: 'C#', ranges: [] });
    await Vue.nextTick();

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(cases)('basic C#%s', async (_, bodyClass) => {
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

function createView({ value, language, ranges }) {
    return new CodeView({
        el: document.createElement('div'),
        propsData: {
            value,
            language,
            ranges
        }
    });
}