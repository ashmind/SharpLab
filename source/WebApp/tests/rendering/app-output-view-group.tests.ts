import Vue from 'vue';
import outputViewGroupSettings from '../../components/internal/app-output-view-group';
import { loadComponentTemplate, themeCases, renderComponent, PickPropTypes } from './helpers';
import * as groups from './data/inspections-groups';

beforeEach(() => {
    loadComponentTemplate('app-output-view-group', 'internal');
    loadComponentTemplate('app-output-view-simple', 'internal');
});
const OutputViewGroup = Vue.component('x', outputViewGroupSettings);

test.each(themeCases)('allocations%s', async (_, bodyClass) => {
    const view = createView({ group: groups.allocations });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ group }: PickPropTypes<typeof OutputViewGroup, 'group'>) {
    return new OutputViewGroup({
        el: document.createElement('div'),
        propsData: { group }
    });
}