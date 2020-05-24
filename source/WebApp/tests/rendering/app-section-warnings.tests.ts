import WarningsSection from '../../components/app-section-warnings';
import { fromPartial } from '../helpers';
import { themeCases, loadComponentTemplate, renderComponent, PickPropTypes } from './helpers';

beforeEach(() => loadComponentTemplate('app-section-warnings'));

test.each(themeCases)('collapsed%s', async (_, bodyClass) => {
    const section = createSection({ warnings: [fromPartial({})] });
    section.$el.classList.add('collapsed');

    const rendered = await renderComponent(section, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(themeCases)('expanded%s', async (_, bodyClass) => {
    const section = createSection({
        warnings: [{ id: 'CS0219', message: "The variable 'test' is assigned but its value is never used", severity: 'warning' }]
    });
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