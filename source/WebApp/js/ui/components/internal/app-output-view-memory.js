import '../app-select.js';

const specialChars = {
    '\r': '\\r',
    '\n': '\\n',
    '\t': '\\t',
    '\0': '\\0'
};

export default {
    props: {
        inspection: Object
    },
    data() {
        return ({
            mode: 'decimal'
        });
    },
    methods: {
        renderCellValue(value) {
            switch (this.mode) {
                case 'decimal': return value.toString().padStart(3, '0');
                case 'hex': return value.toString(16).toUpperCase().padStart(2, '0');
                case 'char': {
                    const char = String.fromCharCode(value);
                    return specialChars[char] || char;
                }
            }
            return '??';
        }
    },
    computed: {
        // TODO: Remove once all backends are updated to use labels
        labels() {
            let labels = this.inspection.labels;
            if (!labels && this.inspection.fields)
                labels = [{ offset: 0, length: this.inspection.data.length, name: 'this branch is not updated to the new label model yet' }];

            return labels;
        },

        labelsWithPadding() {
            const labels = this.labels;
            const results = [];
            for (let i = 0; i < labels.length; i++) {
                const label = labels[i];
                results.push(label);

                const next = labels[i + 1] || { offset: this.inspection.data.length };
                const padding = { offset: label.offset + label.length };
                padding.length = next.offset - padding.offset;
                if (padding.length > 0)
                    results.push(padding);
            }
            return results;
        }
    },
    template: `
      <div class="inspection inspection-memory">
        <header>
          <span class="inspection-title">{{inspection.title}}</span>
          <app-select v-model="mode">
            <option value="decimal">Decimal</option>
            <option value="hex">Hex</option>
            <option value="char">Char</option>
          </app-select>
        </header>
        <table>
          <tr>
            <td v-for="label in labelsWithPadding"
                class="inspection-data-label"
                v-bind:colspan="label.length"
                v-bind:title="label.name">{{label.name}}</td>
          </tr>
          <tr>
            <td v-for="cell in inspection.data"
                class="inspection-data-cell"
                v-bind:class="{ 'inspection-data-zero': (cell === 0) }">{{renderCellValue(cell)}}</td>
          </tr>
        </table>
      </div>
    `.replace(/[\r\n]+\s*/g, '').replace(/\s{2,}/g, ' ')
};