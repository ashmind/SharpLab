import { cases, loadComponentTemplate, renderComponent } from './helpers.js';
import WarningsSection from '../../components/app-warnings-section.js';

beforeEach(() => loadComponentTemplate('app-warnings-section'));

test.each(cases)('collapsed%s', async (_, bodyClass) => {
    const section = createSection({ warnings: [{}] });

    const rendered = await renderComponent(section, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

test.each(cases)('expanded%s', async (_, bodyClass) => {
    const section = createSection({
        warnings: [{ id: 'CS0219', message: "The variable 'test' is assigned but its value is never used" }]
    });
    section.$el.classList.remove('collapsed');

    const rendered = await renderComponent(section, { bodyClass });

    expect(rendered).toMatchImageSnapshot();
});

function createSection({ warnings }) {
    return new WarningsSection({
        el: document.createElement('div'),
        propsData: {
            warnings
        }
    });
}