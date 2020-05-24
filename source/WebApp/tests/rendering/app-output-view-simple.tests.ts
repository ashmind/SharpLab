import Vue from 'vue';
import outputViewSimpleSettings from '../../components/internal/app-output-view-simple';
import { loadComponentTemplate, themeCases, renderComponent, PickPropTypes } from './helpers';

beforeEach(() => loadComponentTemplate('app-output-view-simple', 'internal'));
const OutputViewSimple = Vue.component('x', outputViewSimpleSettings);

test.each(themeCases)('simple%s', async (_, bodyClass) => {
    const view = createView({ inspection: {
        type: 'inspection:simple',
        title: 'Inspect',
        value: 'test'
    } });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('multiline%s', async (_, bodyClass) => {
    const view = createView({ inspection: {
        type: 'inspection:simple',
        title: 'Inspect',
        value: 'Line 1\r\nLine 2\r\nLine 3'
    } });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('exception%s', async (_, bodyClass) => {
    const view = createView({ inspection: {
        type: 'inspection:simple',
        title: 'Exception',
        value: 'System.Exception: test\r\n   at Program.Main()'
    } });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('warning%s', async (_, bodyClass) => {
    const view = createView({ inspection: {
        type: 'inspection:simple',
        title: 'Warning',
        value: 'Please do not rely on Stopwatch results in SharpLab.\r\n\r\nThere are many checks and reports added to your code before it runs,\r\nso the performance might be completely unrelated to the original code.'
    } });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});


function createView({ inspection }: PickPropTypes<typeof OutputViewSimple, 'inspection'>) {
    return new OutputViewSimple({
        el: document.createElement('div'),
        propsData: { inspection }
    });
}