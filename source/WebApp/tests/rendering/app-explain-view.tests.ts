import ExplainView from '../../components/app-explain-view';
import { themeCases, loadComponentTemplate, renderComponent, PickPropTypes } from './helpers';

beforeEach(() => loadComponentTemplate('app-explain-view'));

test.each(themeCases)('empty%s', async (_, bodyClass) => {
    const view = createView({ explanations: [] });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('with explanations%s', async (_, bodyClass) => {
    const explanations = [{
        code: 'in int i',
        name: 'in parameter',
        text: 'In parameters are in essence `ref readonly` — passed by reference, but cannot be changed by the method.  \nThe goal is often to improve performance, as passing large `struct` by reference is faster than copying a value.\n\nNote that passing non-`readonly` `struct` using `in` might cause an overhead — see the docs for details.\n',
        link: 'https://docs.microsoft.com/en-us/dotnet/csharp/reference-semantics-with-value-types#passing-arguments-by-readonly-reference'
    }] as const;
    const view = createView({ explanations });

    const rendered = await renderComponent(view, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createView({ explanations = [] }: PickPropTypes<typeof ExplainView, 'explanations'>) {
    return new ExplainView({
        el: document.createElement('div'),
        propsData: {
            explanations
        }
    });
}