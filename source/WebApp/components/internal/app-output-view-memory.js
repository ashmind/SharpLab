import '../app-select.js';

const specialChars = {
    '\r': '\\r',
    '\n': '\\n',
    '\t': '\\t',
    '\0': '\\0'
};

const settings = {
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
        labelLevels() {
            const levels = [];
            addLabelsToLevelRecursive(levels, this.inspection.labels, 0);
            applyCrossLevelSpansToLabels(levels);
            for (let i = 0; i < levels.length; i++) {
                levels[i] = sortAndAddPaddingBetweenLabels(levels[i], this.inspection.data.length);
            }
            return levels;
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
          <tr v-for="labels in labelLevels">
            <td v-for="label in labels"
                class="inspection-data-label"
                v-bind:colspan="label.length"
                v-bind:rowspan="label.levelSpan"
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

function addLabelsToLevelRecursive(levels, labels, index) {
    let level = levels[index];
    if (!level) {
        level = [];
        levels[index] = level;
    }
    for (const { name, offset, length, nested } of labels) {
        level.push({ name, offset, length, nested });
        if (nested && nested.length > 0)
            addLabelsToLevelRecursive(levels, nested, index + 1);
    }
}

function applyCrossLevelSpansToLabels(levels) {
    for (let i = 0; i < levels.length; i++) {
        for (const label of levels[i]) {
            if (label.nested && label.nested.length > 0)
                continue;
            label.levelSpan = 1;
            for (let j = i + 1; j < levels.length; j++) {
                levels[j].push({ offset: label.offset, length: label.length, levelSpanPlaceholder: true });
                label.levelSpan += 1;
            }
        }
    }
}

function sortAndAddPaddingBetweenLabels(labels, dataLength) {
    const results = [];
    labels.sort((a, b) => {
        if (a.offset > b.offset) return +1;
        if (a.offset < b.offset) return -1;
        return 0;
    });

    for (let i = 0; i < labels.length; i++) {
        const label = labels[i];
        if (i === 0 && label.offset > 0)
            results.push({ offset: 0, length: label.offset });

        if (!label.levelSpanPlaceholder)
            results.push(label);

        const next = labels[i + 1] || { offset: dataLength };
        const padding = { offset: label.offset + label.length };
        padding.length = next.offset - padding.offset;
        if (padding.length > 0)
            results.push(padding);
    }
    return results;
}

export default settings;