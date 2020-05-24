import Vue from 'vue';
import outputViewGraphSettings from '../../components/internal/app-output-view-graph';
import { themeCases, loadComponentTemplate, renderComponent, PickPropTypes } from './helpers';

const OutputViewGraph = Vue.component('x', outputViewGraphSettings);

beforeEach(() => loadComponentTemplate('app-output-view-graph', 'internal'));

test.each(themeCases)('empty%s', async (_, bodyClass) => {
    const view = createView({ inspection: {
        type: 'inspection:memory-graph',
        stack: [],
        heap: [],
        references: []
    } });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

// TODO: this is currently a mess as we can't auto-layout without the sizes
/* test.each(cases)('detailed%s', async (_, bodyClass) => {
    const view = createView({ inspection: detailedInspection });

    const rendered = await renderView(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
}); */

function createView({ inspection }: PickPropTypes<typeof OutputViewGraph, 'inspection'>) {
    return new OutputViewGraph({
        el: document.createElement('div'),
        propsData: { inspection }
    });
}