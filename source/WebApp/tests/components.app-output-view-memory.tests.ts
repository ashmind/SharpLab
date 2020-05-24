import Vue from 'vue';
import outputViewMemorySettings from '../components/internal/app-output-view-memory';

const OutputViewMemory = Vue.component('x', outputViewMemorySettings);

test('app-output-view-memory renders padding correctly', async () => {
    const view = new OutputViewMemory({
        el: document.createElement('x'),
        propsData: {
            inspection: {
                title: '',
                labels: [{ name: 'A', offset: 0, length: 1 }, { name: 'B', offset: 4, length: 4 }],
                data: [0, 0, 0, 0, 0, 0, 0, 0]
            }
        }
    });
    await Vue.nextTick();

    const labelTds = [...view.$el.querySelectorAll('td.inspection-data-label')];

    expect(labelTds.map(l => ({
        label: l.textContent,
        colspan: parseInt(l.getAttribute('colspan')!, 10)
    }))).toEqual([
        { label: 'A', colspan: 1 },
        { label: '',  colspan: 3 },
        { label: 'B', colspan: 4 }
    ]);
});