import Vue from 'vue';
import outputViewMemorySettings from '../../components/internal/app-output-view-memory';
import { cases, renderComponent, PickPropTypes } from './helpers';
import * as inspections from './data/inspections-memory';

const OutputViewMemory = Vue.component('x', outputViewMemorySettings);

test.each(cases)('string%s', async (_, bodyClass) => {
    const view = createView({ inspection: inspections.string });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(cases)('nested%s', async (_, bodyClass) => {
    const view = createView({ inspection: inspections.nested });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ inspection }: PickPropTypes<typeof OutputViewMemory, 'inspection'>) {
    return new OutputViewMemory({
        el: document.createElement('div'),
        propsData: { inspection }
    });
}