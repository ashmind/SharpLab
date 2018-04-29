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

        spaceAfterLastLabel() {
            const inspection = this.inspection;
            // TODO: Remove once all backends are updated to use labels
            if (!inspection.labels)
                return 0;

            const last = inspection.labels[inspection.labels.length - 1];
            if (!last)
                return 0;

            return inspection.data.length - (last.offset + last.length);
        }
    },
    template: `
      <div class="inspection inspection-memory">
        <header>
          <span class="inspection-title">{{inspection.title}} at 0x{{inspection.address}}</span>
          <app-select v-model="mode">
            <option value="decimal">Decimal</option>
            <option value="hex">Hex</option>
            <option value="char">Char</option>
          </app-select>
        </header>
        <table>
          <tr>
            <td v-for="label in labels"
                class="inspection-data-label"
                v-bind:colspan="label.length"
                v-bind:title="label.name">{{label.name}}</td>
            <td v-if="spaceAfterLastLabel > 0"
                class="inspection-data-label"
                v-bind:colspan="spaceAfterLastLabel"></td>
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