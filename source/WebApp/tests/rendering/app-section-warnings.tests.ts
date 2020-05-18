import WarningsSection from '../../components/app-warnings-section';
import { fromPartial } from '../helpers';
import { cases, loadComponentTemplate, renderComponent, PickPropTypes } from './helpers';

beforeEach(() => loadComponentTemplate('app-warnings-section'));

test.each(cases)('collapsed%s', async (_, bodyClass) => {
    const section = createSection({ warnings: [fromPartial({})] });

    const rendered = await renderComponent(section, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(cases)('expanded%s', async (_, bodyClass) => {
    const section = createSection({
        warnings: [{ id: 'CS0219', message: "The variable 'test' is assigned but its value is never used", severity: 'warning' }]
    });
    section.$el.classList.remove('collapsed');

    const rendered = await renderComponent(section, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createSection({ warnings }: PickPropTypes<typeof WarningsSection, 'warnings'>) {
    return new WarningsSection({
        el: document.createElement('div'),
        propsData: {
            warnings
        }
    });
}