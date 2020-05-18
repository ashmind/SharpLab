import Vue from 'vue';
import Select from '../../components/app-select';
import { cases, renderComponent, PickPropTypes } from './helpers';

test.each(cases)('empty%s', async (_, bodyClass) => {
    const select = createSelect({ value: null, options: [] });

    const rendered = await renderComponent(select, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(cases)('with value%s', async (_, bodyClass) => {
    const select = createSelect({
        value: 'test',
        options: [{ text: 'Test', value: 'test' }]
    });

    const rendered = await renderComponent(select, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createSelect({ value, options }: PickPropTypes<typeof Select, 'value'> & {
    options: ReadonlyArray<{
        readonly text: string;
        readonly value: string;
    }>;
}) {
    const select = new Select({
        propsData: { value }
    });

    const vn = new Vue();
    select.$slots.default = options.map(
        o => vn.$createElement('option', { attrs: { value: o.value } }, [o.text])
    );

    select.$mount(document.createElement('div'));
    return select;
}