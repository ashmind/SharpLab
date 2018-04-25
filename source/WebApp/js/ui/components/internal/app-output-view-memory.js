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
        spaceBeforeFirstField() {
            const inspection = this.inspection;
            if (inspection.fields.length === 0)
                return inspection.data.length;

            return inspection.fields[0].offset;
        },

        spaceAfterLastField() {
            const inspection = this.inspection;
            const last = inspection.fields[inspection.fields.length - 1];
            if (!last)
                return 0;

            return inspection.data.length - (last.offset + last.size);
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
            <td v-if="spaceBeforeFirstField > 0"
                class="inspection-field"
                v-bind:colspan="spaceBeforeFirstField"></td>
            <td v-for="field in inspection.fields"
                class="inspection-field"
                v-bind:colspan="field.size"
                v-bind:title="'Field: ' + field.name">{{field.name}}</td>
            <td v-if="spaceAfterLastField > 0"
                class="inspection-field"
                v-bind:colspan="spaceAfterLastField"></td>
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