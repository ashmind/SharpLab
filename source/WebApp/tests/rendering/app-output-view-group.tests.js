import Vue from 'vue';
import { loadComponentTemplate, cases, renderComponent } from './helpers.js';
import * as groups from './data/inspections-groups.js';
import outputViewGroupSettings from '../../components/internal/app-output-view-group.js';

beforeEach(() => {
    loadComponentTemplate('app-output-view-group', 'internal');
    loadComponentTemplate('app-output-view-simple', 'internal');
});
const OutputViewGroup = Vue.component('x', outputViewGroupSettings);

test.each(cases)('allocations%s', async (_, bodyClass) => {
    const view = createView({ group: groups.allocations });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ group }) {
    return new OutputViewGroup({
        el: document.createElement('div'),
        propsData: { group }
    });
}